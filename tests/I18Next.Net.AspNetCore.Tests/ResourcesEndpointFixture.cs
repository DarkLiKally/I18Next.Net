using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.TranslationTrees;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.AspNetCore.Tests;

public class ResourcesEndpointFixture
{
    private const string EnglishTranslation =
        """{"greeting":"Hello {{name}}","menu":{"title":"Menu","items":["Home","About"]},"item_one":"{{count}} item","item_other":"{{count}} items"}""";

    private static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslations("en", "translation", new Dictionary<string, string>
        {
            ["greeting"] = "Hello {{name}}",
            ["menu.title"] = "Menu",
            ["menu.items.0"] = "Home",
            ["menu.items.1"] = "About",
            ["item_one"] = "{{count}} item",
            ["item_other"] = "{{count}} items"
        });
        backend.AddTranslation("de", "translation", "greeting", "Grüß dich {{name}} <3");
        backend.AddTranslation("de", "common", "save", "Speichern");

        return backend;
    }

    private static Task<IHost> StartAsync(ITranslationBackend backend, Action<IEndpointRouteBuilder> configureEndpoints)
    {
        return TestApplication.StartAsync(services => services.AddI18NextLocalization(i18n => i18n.AddBackend(backend)),
            app => app.UseRouting().UseEndpoints(configureEndpoints));
    }

    [Fact]
    public async Task MapI18NextResources_ExistingNamespace_ShouldReturnNestedJson()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());

        var response = await host.GetTestClient().GetAsync("/locales/en/translation.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.ToString().ShouldBe("application/json; charset=utf-8");
        (await response.Content.ReadAsStringAsync()).ShouldBe(EnglishTranslation);
    }

    [Fact]
    public async Task MapI18NextResources_NonAsciiText_ShouldNotEscapeLetters()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());

        var json = await host.GetTestClient().GetStringAsync("/locales/de/translation.json");

        json.ShouldBe("""{"greeting":"Grüß dich {{name}} \u003C3"}""");
    }

    [Fact]
    public async Task MapI18NextResources_RegionalLanguage_ShouldUseBackendFallback()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());

        var json = await host.GetTestClient().GetStringAsync("/locales/de-AT/common.json");

        json.ShouldBe("""{"save":"Speichern"}""");
    }

    [Fact]
    public async Task MapI18NextResources_MissingNamespace_ShouldReturnNotFound()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());

        var response = await host.GetTestClient().GetAsync("/locales/en/common.json");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MapI18NextResources_ConfiguredLanguagesAndNamespaces_ShouldOnlyServeThem()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources(configure: o =>
        {
            o.Languages = ["en"];
            o.Namespaces = ["translation"];
        }));
        var client = host.GetTestClient();

        (await client.GetAsync("/locales/en/translation.json")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/locales/EN/translation.json")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/locales/de/translation.json")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/locales/de/common.json")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/locales/..%2F..%2Fetc/translation.json")]
    [InlineData("/locales/en/..%2F..%2Fsecret.json")]
    [InlineData("/locales/%2E%2E/translation.json")]
    [InlineData("/locales/en/.hidden.json")]
    [InlineData("/locales/en/a..b.json")]
    [InlineData("/locales/e%20n/translation.json")]
    [InlineData("/locales/en/name%5Cother.json")]
    [InlineData("/locales/-en/translation.json")]
    public async Task MapI18NextResources_InvalidNames_ShouldNotReachBackend(string path)
    {
        var backend = Substitute.For<ITranslationBackend>();
        using var host = await StartAsync(backend, e => e.MapI18NextResources());

        var response = await host.GetTestClient().GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await backend.DidNotReceiveWithAnyArgs().LoadNamespaceAsync(null, null);
    }

    [Fact]
    public async Task MapI18NextResources_FileBackend_ShouldServeOriginalStructure()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(directory, "locales", "en"));
        File.WriteAllText(Path.Combine(directory, "locales", "en", "translation.json"),
            """{ "a": { "b": [ "x", { "c": "y" } ] }, "count": 3, "flag": true }""");
        File.WriteAllText(Path.Combine(directory, "secret.json"), """{ "password": "secret" }""");

        try
        {
            using var host = await StartAsync(new JsonFileBackend(Path.Combine(directory, "locales")), e => e.MapI18NextResources());
            var client = host.GetTestClient();

            (await client.GetStringAsync("/locales/en/translation.json")).ShouldBe("""{"a":{"b":["x",{"c":"y"}]},"count":"3","flag":"True"}""");
            (await client.GetAsync("/locales/en/..%2F..%2Fsecret.json")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await client.GetAsync("/locales/..%2F/secret.json")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task MapI18NextResources_MatchingETag_ShouldReturnNotModified()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());
        var client = host.GetTestClient();

        var response = await client.GetAsync("/locales/en/translation.json");
        var eTag = response.Headers.ETag;

        eTag.ShouldNotBeNull();
        eTag.IsWeak.ShouldBeFalse();

        var notModified = await SendAsync(client, "/locales/en/translation.json", eTag.Tag);

        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        notModified.Headers.ETag!.Tag.ShouldBe(eTag.Tag);
        (await notModified.Content.ReadAsStringAsync()).ShouldBeEmpty();

        (await SendAsync(client, "/locales/en/translation.json", "W/" + eTag.Tag)).StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await SendAsync(client, "/locales/en/translation.json", "\"other\", " + eTag.Tag)).StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await SendAsync(client, "/locales/en/translation.json", "*")).StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await SendAsync(client, "/locales/en/translation.json", "\"other\"")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, "/locales/de/translation.json", eTag.Tag)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapI18NextResources_CacheControl_ShouldBeConfigurable()
    {
        using var host = await StartAsync(CreateBackend(), e =>
        {
            e.MapI18NextResources();
            e.MapI18NextResources("/cached/{lng}/{ns}.json", o => o.CacheControl = "public, max-age=3600");
            e.MapI18NextResources("/uncached/{lng}/{ns}.json", o => o.CacheControl = null);
        });
        var client = host.GetTestClient();

        (await client.GetAsync("/locales/en/translation.json")).Headers.CacheControl!.NoCache.ShouldBeTrue();
        (await client.GetAsync("/cached/en/translation.json")).Headers.CacheControl!.MaxAge.ShouldBe(TimeSpan.FromHours(1));
        (await client.GetAsync("/uncached/en/translation.json")).Headers.CacheControl.ShouldBeNull();
    }

    [Fact]
    public async Task MapI18NextResources_Head_ShouldReturnHeadersOnly()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources());

        var response = await host.GetTestClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, "/locales/en/translation.json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        (await response.Content.ReadAsByteArrayAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task MapI18NextResources_MultiLoad_ShouldReturnLanguagesAndNamespaces()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources("/locales/resources.json"));
        var client = host.GetTestClient();

        var json = await client.GetStringAsync("/locales/resources.json?lng=en+de&ns=translation+common");

        json.ShouldBe("""{"en":{"translation":""" + EnglishTranslation + """},"de":{"translation":{"greeting":"Grüß dich {{name}} \u003C3"},"common":{"save":"Speichern"}}}""");
        (await client.GetStringAsync("/locales/resources.json?lng=de%2Ben&ns=common")).ShouldBe("""{"de":{"common":{"save":"Speichern"}}}""");
        (await client.GetAsync("/locales/resources.json?lng=fr&ns=common")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/locales/resources.json?ns=common")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MapI18NextResources_TooManyMultiLoadResources_ShouldReturnBadRequest()
    {
        using var host = await StartAsync(CreateBackend(), e => e.MapI18NextResources("/locales/resources.json"));

        var languages = string.Join("+", Repeat("l", 11));
        var namespaces = string.Join("+", Repeat("n", 10));

        var response = await host.GetTestClient().GetAsync($"/locales/resources.json?lng={languages}&ns={namespaces}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MapI18NextResources_TranslationsChanged_ShouldServeNewTranslations()
    {
        var backend = Substitute.For<INotifyingTranslationBackend>();
        backend.LoadNamespaceAsync("en", "translation").Returns(
            Tree("greeting", "Hello"),
            Tree("greeting", "Hi"));
        backend.LoadNamespaceAsync("en", "common").Returns(Tree("save", "Save"));

        using var host = await StartAsync(backend, e => e.MapI18NextResources());
        var client = host.GetTestClient();

        (await client.GetStringAsync("/locales/en/translation.json")).ShouldBe("""{"greeting":"Hello"}""");
        (await client.GetStringAsync("/locales/en/common.json")).ShouldBe("""{"save":"Save"}""");
        (await client.GetStringAsync("/locales/en/translation.json")).ShouldBe("""{"greeting":"Hello"}""");

        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("de", "translation"));
        (await client.GetStringAsync("/locales/en/translation.json")).ShouldBe("""{"greeting":"Hello"}""");

        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("en", "translation"));
        (await client.GetStringAsync("/locales/en/translation.json")).ShouldBe("""{"greeting":"Hi"}""");
        (await client.GetStringAsync("/locales/en/common.json")).ShouldBe("""{"save":"Save"}""");

        await backend.Received(2).LoadNamespaceAsync("en", "translation");
        await backend.Received(1).LoadNamespaceAsync("en", "common");
    }

    [Fact]
    public async Task MapI18NextResources_ConflictingFlatKeys_ShouldKeepAllValues()
    {
        var backend = Substitute.For<ITranslationBackend>();
        backend.LoadNamespaceAsync("en", "translation").Returns(Task.FromResult<ITranslationTree>(new DictionaryTranslationTree("translation",
            new Dictionary<string, string> { ["a"] = "A", ["a.b"] = "B", ["list.0"] = "x", ["list.2"] = "z" })));

        using var host = await StartAsync(backend, e => e.MapI18NextResources());

        var json = await host.GetTestClient().GetStringAsync("/locales/en/translation.json");

        json.ShouldBe("""{"a":"A","a.b":"B","list":{"0":"x","2":"z"}}""");
    }

    [Fact]
    public async Task MapI18NextResources_InvalidConfiguration_ShouldThrow()
    {
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(CreateBackend(), e => e.MapI18NextResources(configure: o => o.Languages = ["../en"])));
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(CreateBackend(), e => e.MapI18NextResources(configure: o => o.Namespaces = ["a/b"])));
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(CreateBackend(), e => e.MapI18NextResources("")));
    }

    private static Task<ITranslationTree> Tree(string key, string value)
    {
        return Task.FromResult<ITranslationTree>(new DictionaryTranslationTree("translation", new Dictionary<string, string> { [key] = value }));
    }

    private static IEnumerable<string> Repeat(string prefix, int count)
    {
        for (var i = 0; i < count; i++)
            yield return prefix + i;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string path, string ifNoneMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);

        return client.SendAsync(request);
    }
}
