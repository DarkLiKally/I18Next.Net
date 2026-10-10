using System;
using System.Globalization;

using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Blazor.Tests;

public class I18NextBuilderExtensionsFixture
{
    private static ServiceProvider BuildServiceProvider(Action<I18NextBlazorOptions> configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddI18NextLocalization(i18n => i18n
            .IntegrateToBlazor(configure)
            .UseDefaultLanguage("de"));

        return services.BuildServiceProvider();
    }

    [Fact]
    public void IntegrateToBlazor_ShouldRegisterScopedService()
    {
        using var provider = BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<IBlazorI18Next>();

        first.ShouldBeOfType<BlazorI18Next>();
        firstScope.ServiceProvider.GetRequiredService<IBlazorI18Next>().ShouldBeSameAs(first);
        secondScope.ServiceProvider.GetRequiredService<IBlazorI18Next>().ShouldNotBeSameAs(first);
        first.Instance.ShouldBeSameAs(provider.GetRequiredService<II18Next>());
    }

    [Fact]
    public void IntegrateToBlazor_ShouldDetectLanguageFromCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            using var provider = BuildServiceProvider();
            var detector = provider.GetRequiredService<ILanguageDetector>().ShouldBeOfType<ThreadLanguageDetector>();

            detector.FallbackLanguage.ShouldBe("de");

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IBlazorI18Next>().Language.ShouldBe("fr-FR");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void IntegrateToBlazor_Configure_ShouldConfigureOptions()
    {
        using var provider = BuildServiceProvider(o =>
        {
            o.SupportedLanguages = ["de", "en"];
            o.Namespaces = ["translation", "common"];
            o.CookieName = null;
            o.StorageKey = "language";
        });

        var options = provider.GetRequiredService<IOptions<I18NextBlazorOptions>>().Value;

        options.SupportedLanguages.ShouldBe(["de", "en"]);
        options.Namespaces.ShouldBe(["translation", "common"]);
        options.CookieName.ShouldBeNull();
        options.StorageKey.ShouldBe("language");
    }

    [Fact]
    public void IntegrateToBlazor_Defaults_ShouldStoreLanguageInCookieAndLocalStorage()
    {
        using var provider = BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<I18NextBlazorOptions>>().Value;

        options.SupportedLanguages.ShouldBeEmpty();
        options.Namespaces.ShouldBeEmpty();
        options.CookieName.ShouldBe(".AspNetCore.Culture");
        options.StorageKey.ShouldBe("i18nextLng");
        options.AllowedHtmlTags.ShouldContain("strong");
        options.AllowedHtmlTags.ShouldContain("BR");
    }
}
