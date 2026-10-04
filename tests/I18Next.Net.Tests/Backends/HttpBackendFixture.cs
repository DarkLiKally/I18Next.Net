using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class HttpBackendFixture
{
    private readonly FakeHandler _handler = new();
    private readonly HttpBackend _backend;

    public HttpBackendFixture()
    {
        _handler.Responses["https://cdn.example.com/locales/en/translation.json"] =
            """{ "greeting": "Hello {{name}}", "nested": { "key": "Nested" }, "list": [ "a", "b" ], "count": 3, "flag": true }""";
        _handler.Responses["https://cdn.example.com/locales/de/translation.json"] = """{ "greeting": "Hallo {{name}}" }""";

        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://cdn.example.com/") };
        _backend = new HttpBackend(httpClient);
    }

    [Fact]
    public async Task LoadNamespaceAsync_ShouldParseJson()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "translation");

        tree.GetValue("greeting", null).ShouldBe("Hello {{name}}");
        tree.GetValue("nested.key", null).ShouldBe("Nested");
        tree.GetValue("list.1", null).ShouldBe("b");
        tree.GetValue("count", null).ShouldBe("3");
        tree.GetValue("flag", null).ShouldBe("True");
        tree.Namespace.ShouldBe("translation");
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageMissing_ShouldFallBackToLanguagePart()
    {
        var tree = await _backend.LoadNamespaceAsync("de-AT", "translation");

        tree.GetValue("greeting", null).ShouldBe("Hallo {{name}}");
        _handler.RequestedUrls.ShouldBe([
            "https://cdn.example.com/locales/de-AT/translation.json",
            "https://cdn.example.com/locales/de/translation.json"
        ]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_FallbackDisabled_ShouldReturnNull()
    {
        _backend.FallbackToLanguagePart = false;

        (await _backend.LoadNamespaceAsync("de-AT", "translation")).ShouldBeNull();
        _handler.RequestedUrls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoadNamespaceAsync_NotFound_ShouldReturnNull()
    {
        (await _backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
        (await _backend.LoadNamespaceAsync("fr-FR", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_ServerError_ShouldThrow()
    {
        _handler.StatusCodes["https://cdn.example.com/locales/en/broken.json"] = HttpStatusCode.InternalServerError;

        await Should.ThrowAsync<HttpRequestException>(() => _backend.LoadNamespaceAsync("en", "broken"));
    }

    [Fact]
    public async Task LoadNamespaceAsync_LoadPathPlaceholders_ShouldBeEscaped()
    {
        _backend.LoadPath = "/i18n/{{ns}}/{{lng}}";

        await _backend.LoadNamespaceAsync("en", "my ns");

        _handler.RequestedUrls[0].ShouldBe("https://cdn.example.com/i18n/my%20ns/en");
    }

    [Fact]
    public async Task LoadNamespaceAsync_LoadPathResolver_ShouldOverrideLoadPath()
    {
        _backend.LoadPathResolver = (language, ns) => language == "en" ? $"https://other.example.com/{ns}.{language}.json" : null;

        await _backend.LoadNamespaceAsync("en", "translation");
        await _backend.LoadNamespaceAsync("de", "translation");

        _handler.RequestedUrls.ShouldBe([
            "https://other.example.com/translation.en.json",
            "https://cdn.example.com/locales/de/translation.json"
        ]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_QueryStringParams_ShouldBeAppended()
    {
        _backend.QueryStringParams["v"] = "1.2";
        _backend.QueryStringParams["a b"] = "c&d";

        await _backend.LoadNamespaceAsync("en", "translation");

        _backend.LoadPath = "locales/{{lng}}/{{ns}}.json?token=x";
        await _backend.LoadNamespaceAsync("de", "translation");

        _handler.RequestedUrls.ShouldBe([
            "https://cdn.example.com/locales/en/translation.json?v=1.2&a%20b=c%26d",
            "https://cdn.example.com/locales/de/translation.json?token=x&v=1.2&a%20b=c%26d"
        ]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_CustomHeaders_ShouldBeSent()
    {
        _backend.CustomHeaders["Authorization"] = "Bearer token";
        _backend.CustomHeaders["X-Custom"] = "value";

        await _backend.LoadNamespaceAsync("en", "translation");

        _handler.Requests[0].Headers.GetValues("Authorization").Single().ShouldBe("Bearer token");
        _handler.Requests[0].Headers.GetValues("X-Custom").Single().ShouldBe("value");
    }

    [Fact]
    public async Task LoadNamespaceAsync_CustomParse_ShouldBeUsed()
    {
        _handler.Responses["https://cdn.example.com/locales/en/plain.json"] = "first=One\nsecond=Two";
        _backend.Parse = (content, builder) =>
        {
            foreach (var line in content.Split('\n'))
            {
                var parts = line.Split('=');
                builder.AddTranslation(parts[0], parts[1]);
            }

            return builder.Build();
        };

        var tree = await _backend.LoadNamespaceAsync("en", "plain");

        tree.GetValue("second", null).ShouldBe("Two");
    }

    [Fact]
    public async Task LoadNamespaceAsync_ClientProvider_ShouldBeCalledPerRequest()
    {
        var calls = 0;
        var backend = new HttpBackend(() =>
        {
            calls++;

            return new HttpClient(_handler, false) { BaseAddress = new Uri("https://cdn.example.com/") };
        });

        await backend.LoadNamespaceAsync("en", "translation");
        await backend.LoadNamespaceAsync("de", "translation");

        calls.ShouldBe(2);
    }

    [Fact]
    public async Task LoadNamespaceAsync_FlatTreeBuilder_ShouldBeUsed()
    {
        var backend = new HttpBackend(() => new HttpClient(_handler, false) { BaseAddress = new Uri("https://cdn.example.com/") },
            HttpBackend.DefaultLoadPath, new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.GetValue("nested.key", null).ShouldBe("Nested");
    }

    [Fact]
    public void Constructor_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new HttpBackend((HttpClient)null));
        Should.Throw<ArgumentNullException>(() => new HttpBackend((Func<HttpClient>)null));
        Should.Throw<ArgumentNullException>(() => new HttpBackend(() => null, null));
        Should.Throw<ArgumentNullException>(() => new HttpBackend(() => null, "path", null));
        new HttpBackend().LoadPath.ShouldBe(HttpBackend.DefaultLoadPath);
    }

    [Fact]
    public async Task I18Next_WithHttpBackend_ShouldTranslate()
    {
        var i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend)) { Language = "de-AT" };
        i18Next.SetFallbackLanguages("en");

        (await i18Next.Ta("greeting", new { name = "Anna" })).ShouldBe("Hallo Anna");
        (await i18Next.Ta("nested.key")).ShouldBe("Nested");
    }

    [Fact]
    public async Task AddHttpBackend_ShouldUseHttpClientFactory()
    {
        _handler.Responses["https://cdn.example.com/locales/en/translation.json?v=2"] = """{ "greeting": "Hello {{name}}" }""";

        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18N => i18N
            .AddHttpBackend("locales/{{lng}}/{{ns}}.json",
                backend => backend.QueryStringParams["v"] = "2",
                client => client
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://cdn.example.com/"))
                    .ConfigurePrimaryHttpMessageHandler(() => _handler)));

        using var provider = services.BuildServiceProvider();

        var backend = provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<HttpBackend>();
        var tree = await backend.LoadNamespaceAsync("en", "translation");

        _handler.RequestedUrls.ShouldBe(["https://cdn.example.com/locales/en/translation.json?v=2"]);
        tree.GetValue("greeting", null).ShouldBe("Hello {{name}}");
    }

    private class FakeHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> RequestedUrls { get; } = [];

        public Dictionary<string, string> Responses { get; } = [];

        public Dictionary<string, HttpStatusCode> StatusCodes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;

            Requests.Add(request);
            RequestedUrls.Add(url);

            if (StatusCodes.TryGetValue(url, out var statusCode))
                return Task.FromResult(new HttpResponseMessage(statusCode));

            return Task.FromResult(Responses.TryGetValue(url, out var content)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
