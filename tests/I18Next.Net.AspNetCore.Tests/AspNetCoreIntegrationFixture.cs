using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

using Xunit;

namespace I18Next.Net.AspNetCore.Tests;

public class AspNetCoreIntegrationFixture
{
    private static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}!");
        backend.AddTranslation("de", "translation", "greeting", "Hallo {{name}}!");

        return backend;
    }

    [Fact]
    public void IntegrateToAspNetCore_ShouldUseThreadCultureAndHtmlInterpolation()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.IntegrateToAspNetCore().AddBackend(CreateBackend()));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILanguageDetector>().ShouldBeOfType<ThreadLanguageDetector>();
        provider.GetRequiredService<IInterpolator>().ShouldBeOfType<HtmlInterpolator>();
        provider.GetRequiredService<II18Next>().DetectLanguageOnEachTranslation.ShouldBeTrue();
    }

    [Fact]
    public void AddI18NextViewLocalization_ShouldReplaceHtmlLocalizerFactory()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(CreateBackend()));
        services.AddMvc().AddViewLocalization().AddI18NextViewLocalization();

        services.Count(s => s.ServiceType == typeof(IHtmlLocalizerFactory)).ShouldBe(1);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IHtmlLocalizerFactory>().ShouldBeOfType<I18NextHtmlLocalizerFactory>();
    }

    [Fact]
    public void AddI18NextViewLocalization_WithoutViewLocalization_ShouldAddIt()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(CreateBackend()));
        services.AddMvc().AddI18NextViewLocalization();

        services.ShouldContain(s => s.ServiceType == typeof(IViewLocalizer));
        services.Single(s => s.ServiceType == typeof(IHtmlLocalizerFactory)).ImplementationType.ShouldBe(typeof(I18NextHtmlLocalizerFactory));
    }

    [Fact]
    public void HtmlLocalizer_ShouldEncodeArgumentsOnly()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddInterpolator<HtmlInterpolator>().AddBackend(CreateBackend()).UseDefaultLanguage("en"));

        using var provider = services.BuildServiceProvider();
        var factory = new I18NextHtmlLocalizerFactory(provider.GetRequiredService<IStringLocalizerFactory>());
        var localizer = factory.Create(typeof(AspNetCoreIntegrationFixture));

        localizer["greeting", new { name = "<b>Jane</b>" }].Value.ShouldBe("Hello &lt;b&gt;Jane&lt;&#x2F;b&gt;!");
        factory.Create("base", "location")["missing"].IsResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void HtmlLocalizer_InvalidArguments_ShouldThrow()
    {
        var factory = new I18NextHtmlLocalizerFactory(new I18NextStringLocalizerFactory(new I18NextNet(CreateBackend(), new DefaultTranslator(CreateBackend()))));

        Should.Throw<ArgumentNullException>(() => new I18NextHtmlLocalizerFactory(null));
        Should.Throw<ArgumentNullException>(() => factory.Create(null));
        Should.Throw<ArgumentNullException>(() => factory.Create(null, "location"));
        Should.Throw<ArgumentNullException>(() => factory.Create("base", null));
        Should.Throw<ArgumentNullException>(() => factory.Create(typeof(AspNetCoreIntegrationFixture))[null, "argument"]);
    }

    [Fact]
    public async Task RequestLocalization_ShouldTranslateInRequestLanguage()
    {
        using var host = await TestApplication.StartAsync(
            services => services.AddI18NextLocalization(i18n => i18n.IntegrateToAspNetCore().AddBackend(CreateBackend()).UseDefaultLanguage("en")),
            app => app
                .UseRequestLocalization(o => o.AddSupportedCultures("en", "de").AddSupportedUICultures("en", "de").SetDefaultCulture("en"))
                .UseRouting()
                .UseEndpoints(e => e.MapGet("/", (II18Next i18n, IStringLocalizer localizer) => $"{i18n.T("greeting", new { name = "Jane" })} {localizer["greeting", new { name = "Joe" }]}")));
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("Accept-Language", "de-DE, en;q=0.5");

        (await (await client.SendAsync(request)).Content.ReadAsStringAsync()).ShouldBe("Hallo Jane! Hallo Joe!");
        (await client.GetStringAsync("/")).ShouldBe("Hello Jane! Hello Joe!");
    }
}
