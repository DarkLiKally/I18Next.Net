using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class MissingKeyHandlersFixture : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Fact]
    public void FileMissingKeyHandler_ShouldWriteNestedKeysWithDefaultValues()
    {
        var handler = new FileMissingKeyHandler(Path.Combine(_directory, "{{lng}}", "{{ns}}.missing.json"));
        var backend = new InMemoryBackend();
        backend.AddTranslation("de", "translation", "existing", "Vorhanden");
        backend.AddTranslation("de", "common", "existing", "Vorhanden");
        var translator = new DefaultTranslator(backend);
        translator.MissingKeyHandlers.Add(handler);
        var i18Next = new I18NextNet(backend, translator) { Language = "de" };

        i18Next.T("menu.title");
        i18Next.T("menu.save", new { defaultValue = "Speichern" });
        i18Next.T("common:ok");
        i18Next.T("menu.title");

        File.ReadAllText(Path.Combine(_directory, "de", "translation.missing.json")).Replace("\r\n", "\n")
            .ShouldBe("{\n  \"menu\": {\n    \"title\": \"menu.title\",\n    \"save\": \"Speichern\"\n  }\n}");
        File.ReadAllText(Path.Combine(_directory, "de", "common.missing.json")).ShouldContain("\"ok\": \"ok\"");
    }

    [Fact]
    public async Task FileMissingKeyHandler_ExistingFile_ShouldKeepValues()
    {
        var path = Path.Combine(_directory, "en.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """{ "menu": "Menu", "title": { "main": "Grüße" } }""");

        var handler = new FileMissingKeyHandler(Path.Combine(_directory, "{{lng}}.json"));

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "menu.open", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "title.main", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "menu", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "title.sub", []) { DefaultValue = "Sub" });

        var content = File.ReadAllText(path);

        content.ShouldContain("\"menu.open\": \"menu.open\"");
        content.ShouldContain("\"main\": \"Grüße\"");
        content.ShouldContain("\"sub\": \"Sub\"");
        handler.AddPath.ShouldBe(Path.Combine(_directory, "{{lng}}.json"));
    }

    [Fact]
    public async Task FileMissingKeyHandler_FlatKeys_ShouldNotNest()
    {
        var handler = new FileMissingKeyHandler(Path.Combine(_directory, "{{lng}}-{{ns}}.json")) { KeySeparator = null };

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "menu.title", []));

        File.ReadAllText(Path.Combine(_directory, "en-translation.json")).ShouldContain("\"menu.title\": \"menu.title\"");
    }

    [Theory]
    [InlineData("..", "translation")]
    [InlineData("en/..", "translation")]
    [InlineData("en", "..\\x")]
    [InlineData("", "translation")]
    [InlineData("c:", "translation")]
    public async Task FileMissingKeyHandler_InvalidPathSegments_ShouldBeIgnored(string language, string ns)
    {
        var handler = new FileMissingKeyHandler(Path.Combine(_directory, "{{lng}}", "{{ns}}.json"));

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs(language, ns, "key", []));

        Directory.Exists(_directory).ShouldBeFalse();
    }

    [Fact]
    public void FileMissingKeyHandler_NullPath_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new FileMissingKeyHandler(null));
        new FileMissingKeyHandler().AddPath.ShouldBe(FileMissingKeyHandler.DefaultAddPath);
    }

    [Fact]
    public async Task HttpMissingKeyHandler_ShouldPostKeyAndDefaultValue()
    {
        var messageHandler = new RecordingHandler(HttpStatusCode.OK);
        var handler = new HttpMissingKeyHandler(new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com/") });
        handler.CustomHeaders["Authorization"] = "Bearer abc";

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de-AT", "my ns", "menu.save", []) { DefaultValue = "Speichern" });
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de-AT", "my ns", "menu.save", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de-AT", "my ns", "menu.open", []));

        messageHandler.Requests.Count.ShouldBe(2);
        messageHandler.Requests[0].Url.ShouldBe("https://example.com/locales/add/de-AT/my%20ns");
        messageHandler.Requests[0].Body.ShouldBe("""{"menu.save":"Speichern"}""");
        messageHandler.Requests[0].ContentType.ShouldBe("application/json; charset=utf-8");
        messageHandler.Requests[0].Authorization.ShouldBe("Bearer abc");
        messageHandler.Requests[1].Body.ShouldBe("""{"menu.open":"menu.open"}""");
    }

    [Fact]
    public async Task HttpMissingKeyHandler_Failure_ShouldLogAndRetryLater()
    {
        var messageHandler = new RecordingHandler(HttpStatusCode.InternalServerError);
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var handler = new HttpMissingKeyHandler(() => new HttpClient(messageHandler), "https://example.com/add/{{lng}}/{{ns}}") { Logger = logger };

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "key", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "key", []));

        messageHandler.Requests.Count.ShouldBe(2);
        logger.Received(2).Log(LogLevel.Warning, Arg.Any<HttpRequestException>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public void HttpMissingKeyHandler_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new HttpMissingKeyHandler((HttpClient)null));
        Should.Throw<ArgumentNullException>(() => new HttpMissingKeyHandler((Func<HttpClient>)null));
        Should.Throw<ArgumentNullException>(() => new HttpMissingKeyHandler((string)null));
        new HttpMissingKeyHandler().AddPath.ShouldBe(HttpMissingKeyHandler.DefaultAddPath);
    }

    [Fact]
    public async Task MetricsMissingKeyHandler_ShouldCountWithTags()
    {
        using var meter = new Meter("I18Next.Net.Tests." + Guid.NewGuid());
        var measurements = new List<(long Value, Dictionary<string, object> Tags)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == meter)
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => measurements.Add((value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.Start();

        var handler = new MetricsMissingKeyHandler(meter);
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "key", []));

        handler.IncludeKey = true;
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "common", "other", []));

        measurements.Count.ShouldBe(2);
        measurements[0].Value.ShouldBe(1);
        measurements[0].Tags["i18next.language"].ShouldBe("de");
        measurements[0].Tags["i18next.namespace"].ShouldBe("translation");
        measurements[0].Tags.ContainsKey("i18next.key").ShouldBeFalse();
        measurements[1].Tags["i18next.key"].ShouldBe("other");
    }

    [Fact]
    public async Task Builder_ShouldRegisterMissingKeyHandlers()
    {
        var messageHandler = new RecordingHandler(HttpStatusCode.OK);
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "existing", "Existing");
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(backend)
            .UseDefaultLanguage("en")
            .SaveMissingKeysToFiles(Path.Combine(_directory, "{{lng}}", "{{ns}}.missing.json"))
            .SaveMissingKeysOverHttp("https://example.com/add/{{lng}}/{{ns}}", h => h.CustomHeaders["X-Test"] = "1",
                c => c.ConfigurePrimaryHttpMessageHandler(() => messageHandler))
            .AddMissingKeyMetrics(true));

        using var provider = services.BuildServiceProvider();

        var handlers = provider.GetServices<IMissingKeyHandler>().ToList();
        handlers.Count.ShouldBe(3);
        handlers[0].ShouldBeOfType<FileMissingKeyHandler>();
        handlers[1].ShouldBeOfType<HttpMissingKeyHandler>().CustomHeaders["X-Test"].ShouldBe("1");
        handlers[2].ShouldBeOfType<MetricsMissingKeyHandler>().IncludeKey.ShouldBeTrue();

        provider.GetRequiredService<II18Next>().T("missing").ShouldBe("missing");

        File.Exists(Path.Combine(_directory, "en", "translation.missing.json")).ShouldBeTrue();
        messageHandler.Requests.Single().Url.ShouldBe("https://example.com/add/en/translation");
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public List<(string Url, string Body, string ContentType, string Authorization)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri.AbsoluteUri, await request.Content.ReadAsStringAsync(), request.Content.Headers.ContentType?.ToString(),
                request.Headers.TryGetValues("Authorization", out var values) ? values.Single() : null));

            return new HttpResponseMessage(statusCode);
        }
    }
}
