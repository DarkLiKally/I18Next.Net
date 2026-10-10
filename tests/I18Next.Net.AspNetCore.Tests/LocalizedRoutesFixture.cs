using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.TranslationTrees;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.AspNetCore.Tests;

public class LocalizedRoutesFixture
{
    private static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("de", "routes", "products", "produkte");
        backend.AddTranslation("de", "routes", "about", "über-uns");
        backend.AddTranslation("de", "translation", "title", "Produkt");
        backend.AddTranslation("en", "translation", "title", "Product");

        return backend;
    }

    private static Task<IHost> StartAsync(Action<I18NextLocalizedRoutesOptions> configure, ITranslationBackend backend = null,
        Action<IApplicationBuilder> configureBefore = null, Action<IApplicationBuilder> configureAfter = null)
    {
        return TestApplication.StartAsync(
            services => services.AddI18NextLocalization(i18n => i18n.IntegrateToAspNetCore().AddBackend(backend ?? CreateBackend()).UseDefaultLanguage("en")),
            app =>
            {
                configureBefore?.Invoke(app);
                app.UseI18NextLocalizedRoutes(configure);
                configureAfter?.Invoke(app);
                app.UseRouting();
                app.UseEndpoints(MapEndpoints);
            });
    }

    private static void ConfigureLanguages(I18NextLocalizedRoutesOptions options)
    {
        options.Languages = ["en", "de"];
    }

    private static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "home");
        endpoints.MapGet("/about", () => "about");
        endpoints.MapGet("/api/status", () => "ok");
        endpoints.MapPost("/products", () => "created");
        endpoints.MapGet("/products/{id}", (HttpContext context, string id, II18Next i18n) =>
            $"{i18n.T("title")} {id} {CultureInfo.CurrentCulture.Name} {CultureInfo.CurrentUICulture.Name} {context.Request.PathBase}|{context.Request.Path}{context.Request.QueryString}");
        endpoints.MapGet("/products/{id}/links", (HttpContext context) =>
            $"{context.GetLocalizedPath("/products/7?page=2#top")} {context.GetLocalizedPath("/about", "de-AT")} {context.GetLocalizedRequestPath("en")} {context.GetLocalizedRequestPath("de")}");
        endpoints.MapGet("/culture", (HttpContext context) => context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? "none");
    }

    [Theory]
    [InlineData("/de/produkte/42", "Produkt 42 de de /de|/products/42")]
    [InlineData("/en/products/42", "Product 42 en en /en|/products/42")]
    [InlineData("/DE/Produkte/42", "Produkt 42 de de /DE|/products/42")]
    [InlineData("/de/products/42", "Produkt 42 de de /de|/products/42")]
    [InlineData("/de/produkte/42?sort=name&q=a%20b", "Produkt 42 de de /de|/products/42?sort=name&q=a%20b")]
    public async Task UseI18NextLocalizedRoutes_LocalizedPath_ShouldMatchCanonicalEndpoint(string path, string expected)
    {
        using var host = await StartAsync(ConfigureLanguages);

        var body = await host.GetTestClient().GetStringAsync(path);

        body.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/de/%C3%BCber-uns", "about")]
    [InlineData("/de/about", "about")]
    [InlineData("/de", "home")]
    [InlineData("/de/", "home")]
    [InlineData("/en", "home")]
    public async Task UseI18NextLocalizedRoutes_SpecialPaths_ShouldMatchCanonicalEndpoint(string path, string expected)
    {
        using var host = await StartAsync(ConfigureLanguages);

        var body = await host.GetTestClient().GetStringAsync(path);

        body.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/de/unbekannt/42")]
    [InlineData("/en/produkte/42")]
    [InlineData("/fr/products/42")]
    [InlineData("/details/products/42")]
    public async Task UseI18NextLocalizedRoutes_UnknownSegments_ShouldPassThroughUntouched(string path)
    {
        using var host = await StartAsync(ConfigureLanguages);

        var response = await host.GetTestClient().GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_MissingPrefixWithoutRedirect_ShouldPassThrough()
    {
        using var host = await StartAsync(ConfigureLanguages);

        var response = await host.GetTestClient().GetAsync("/products/42");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEndWith(" |/products/42");
    }

    [Theory]
    [InlineData("/products/42?page=2", "fr;q=0.9, de;q=0.8, en;q=0.1", "/de/produkte/42?page=2")]
    [InlineData("/products/42", "de-AT", "/de/produkte/42")]
    [InlineData("/products/42", "en-GB,de;q=0.5", "/en/products/42")]
    [InlineData("/products/42", "de;q=0, fr", "/en/products/42")]
    [InlineData("/products/42", null, "/en/products/42")]
    [InlineData("/", "de", "/de")]
    [InlineData("/about/", "de", "/de/%C3%BCber-uns/")]
    public async Task UseI18NextLocalizedRoutes_MissingPrefixWithRedirect_ShouldRedirectToDetectedLanguage(string path, string acceptLanguage, string expected)
    {
        using var host = await StartAsync(o =>
        {
            ConfigureLanguages(o);
            o.MissingLanguagePrefix = MissingLanguagePrefixBehavior.RedirectToDetectedLanguage;
        });
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (acceptLanguage != null)
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        var response = await host.GetTestClient().SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe(expected);
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_RedirectWithRequestLocalization_ShouldUseRequestCulture()
    {
        using var host = await StartAsync(o =>
            {
                ConfigureLanguages(o);
                o.DefaultLanguage = "de";
                o.MissingLanguagePrefix = MissingLanguagePrefixBehavior.RedirectToDetectedLanguage;
            },
            configureBefore: app => app.UseRequestLocalization(o => o
                .AddSupportedCultures("en", "de")
                .AddSupportedUICultures("en", "de")
                .SetDefaultCulture("de")));
        var client = host.GetTestClient();

        var response = await client.GetAsync("/products/42?culture=en");

        response.Headers.Location!.OriginalString.ShouldBe("/en/products/42?culture=en");
        (await client.GetAsync("/products/42")).Headers.Location!.OriginalString.ShouldBe("/de/produkte/42");
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_RedirectExclusions_ShouldPassThrough()
    {
        using var host = await StartAsync(o =>
        {
            ConfigureLanguages(o);
            o.MissingLanguagePrefix = MissingLanguagePrefixBehavior.RedirectToDetectedLanguage;
            o.ExcludedPaths = ["/api"];
        });
        var client = host.GetTestClient();

        (await client.GetStringAsync("/api/status")).ShouldBe("ok");
        (await client.GetAsync("/API/status")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync("/products", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/apis")).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GetLocalizedPath_InsideLocalizedRequest_ShouldCreateLocalizedLinks()
    {
        using var host = await StartAsync(ConfigureLanguages);
        var client = host.GetTestClient();

        (await client.GetStringAsync("/de/produkte/42/links?tab=1"))
            .ShouldBe("/de/produkte/7?page=2#top /de/%C3%BCber-uns /en/products/42/links?tab=1 /de/produkte/42/links?tab=1");
        (await client.GetStringAsync("/en/products/42/links"))
            .ShouldBe("/en/products/7?page=2#top /de/%C3%BCber-uns /en/products/42/links /de/produkte/42/links");
    }

    [Fact]
    public async Task GetLocalizedPath_WithPathBase_ShouldKeepPathBase()
    {
        using var host = await StartAsync(ConfigureLanguages, configureBefore: app => app.UsePathBase("/shop"));
        var client = host.GetTestClient();

        (await client.GetStringAsync("/shop/de/produkte/42")).ShouldBe("Produkt 42 de de /shop/de|/products/42");
        (await client.GetStringAsync("/shop/de/produkte/42/links"))
            .ShouldBe("/shop/de/produkte/7?page=2#top /shop/de/%C3%BCber-uns /shop/en/products/42/links /shop/de/produkte/42/links");
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_RedirectWithPathBase_ShouldKeepPathBase()
    {
        using var host = await StartAsync(o =>
            {
                ConfigureLanguages(o);
                o.MissingLanguagePrefix = MissingLanguagePrefixBehavior.RedirectToDetectedLanguage;
            },
            configureBefore: app => app.UsePathBase("/shop"));

        var response = await host.GetTestClient().GetAsync("/shop/products/42");

        response.Headers.Location!.OriginalString.ShouldBe("/shop/en/products/42");
    }

    [Fact]
    public async Task PathLanguageRequestCultureProvider_AfterLocalizedRoutes_ShouldUsePrefixLanguage()
    {
        using var host = await StartAsync(ConfigureLanguages, configureAfter: app => app.UseRequestLocalization(o =>
        {
            o.AddSupportedCultures("en", "de").AddSupportedUICultures("en", "de").SetDefaultCulture("en");
            o.RequestCultureProviders.Insert(0, new PathLanguageRequestCultureProvider());
        }));
        var client = host.GetTestClient();

        (await SendWithLanguageAsync(client, "/de/culture", "en")).ShouldBe("de");
        (await SendWithLanguageAsync(client, "/culture", "de")).ShouldBe("de");
        (await SendWithLanguageAsync(client, "/de/produkte/42", "en")).ShouldStartWith("Produkt 42 de de");
    }

    [Fact]
    public async Task PathLanguageRequestCultureProvider_BeforeLocalizedRoutes_ShouldUsePrefixLanguage()
    {
        using var host = await StartAsync(ConfigureLanguages, configureBefore: app => app.UseRequestLocalization(o =>
        {
            o.AddSupportedCultures("en", "de").AddSupportedUICultures("en", "de").SetDefaultCulture("en");
            o.RequestCultureProviders.Insert(0, new PathLanguageRequestCultureProvider());
        }));
        var client = host.GetTestClient();

        (await SendWithLanguageAsync(client, "/de/culture", "en")).ShouldBe("de");
        (await SendWithLanguageAsync(client, "/culture", "de")).ShouldBe("de");
        (await SendWithLanguageAsync(client, "/products/42", "en")).ShouldStartWith("Product 42 en en");
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_TranslationsChanged_ShouldUseNewSegments()
    {
        var backend = Substitute.For<INotifyingTranslationBackend>();
        backend.LoadNamespaceAsync("de", "routes").Returns(Tree("products", "produkte"), Tree("products", "artikel"));
        backend.LoadNamespaceAsync("en", "routes").Returns(Task.FromResult<ITranslationTree>(null));

        using var host = await StartAsync(ConfigureLanguages, backend);
        var client = host.GetTestClient();

        (await client.GetAsync("/de/produkte/42")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/de/artikel/42")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("en", "routes"));
        (await client.GetAsync("/de/produkte/42")).StatusCode.ShouldBe(HttpStatusCode.OK);

        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("de", "routes"));
        (await client.GetAsync("/de/artikel/42")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/de/produkte/42")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        await backend.Received(2).LoadNamespaceAsync("de", "routes");
    }

    [Fact]
    public async Task UseI18NextLocalizedRoutes_FailingBackend_ShouldLoadAgain()
    {
        var backend = Substitute.For<ITranslationBackend>();
        backend.LoadNamespaceAsync("de", "routes").Returns(
            Task.FromException<ITranslationTree>(new InvalidOperationException("Backend unavailable")),
            Tree("products", "produkte"));
        backend.LoadNamespaceAsync("en", "routes").Returns(Task.FromResult<ITranslationTree>(null));

        using var host = await StartAsync(ConfigureLanguages, backend);
        var client = host.GetTestClient();

        await Should.ThrowAsync<InvalidOperationException>(() => client.GetAsync("/de/produkte/42"));
        (await client.GetAsync("/de/produkte/42")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void GetLocalizedPath_WithoutMiddleware_ShouldThrow()
    {
        var context = new DefaultHttpContext();

        Should.Throw<InvalidOperationException>(() => context.GetLocalizedPath("/products"));
        Should.Throw<InvalidOperationException>(() => context.GetLocalizedRequestPath("de"));
        Should.Throw<ArgumentNullException>(() => ((HttpContext)null).GetLocalizedPath("/products"));
    }

    private static async Task<string> SendWithLanguageAsync(HttpClient client, string path, string acceptLanguage)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        var response = await client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    private static Task<ITranslationTree> Tree(string key, string value)
    {
        return Task.FromResult<ITranslationTree>(new DictionaryTranslationTree("routes", new Dictionary<string, string> { [key] = value }));
    }
}
