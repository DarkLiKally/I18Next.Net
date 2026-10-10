using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace I18Next.Net.Blazor.Tests;

public class LanguageSelectorFixture : BunitContext
{
    public LanguageSelectorFixture()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IBlazorI18Next I18n => Services.GetRequiredService<IBlazorI18Next>();

    [Fact]
    public void Render_ShouldListSupportedLanguagesWithNativeNames()
    {
        Services.AddTestI18Next(detectedLanguage: "de", configure: o => o.SupportedLanguages = ["en", "de", "fr"]);

        var cut = Render<LanguageSelector>();

        var options = cut.FindAll("option");
        options.Select(o => o.GetAttribute("value")).ShouldBe(["en", "de", "fr"]);
        options.Select(o => o.TextContent).ShouldBe(["English", "Deutsch", "Français"]);
        cut.Find("select").GetAttribute("value").ShouldBe("de");
    }

    [Fact]
    public void Render_NoSupportedLanguages_ShouldListCurrentLanguage()
    {
        Services.AddTestI18Next(detectedLanguage: "de", configure: _ => { });

        var cut = Render<LanguageSelector>();

        cut.FindAll("option").Select(o => o.GetAttribute("value")).ShouldBe(["de"]);
    }

    [Fact]
    public void Render_LanguagesAndDisplayName_ShouldBeUsed()
    {
        Services.AddTestI18Next();

        var cut = Render<LanguageSelector>(p => p
            .Add(c => c.Languages, ["de", "en"])
            .Add(c => c.DisplayName, l => l.ToUpperInvariant()));

        cut.FindAll("option").Select(o => o.TextContent).ShouldBe(["DE", "EN"]);
    }

    [Fact]
    public void Render_UnknownCulture_ShouldDisplayLanguage()
    {
        Services.AddTestI18Next();

        var cut = Render<LanguageSelector>(p => p.Add(c => c.Languages, ["en", "not a language"]));

        cut.FindAll("option")[1].TextContent.ShouldBe("not a language");
    }

    [Fact]
    public void Render_AdditionalAttributes_ShouldBeApplied()
    {
        Services.AddTestI18Next();

        var cut = Render<LanguageSelector>(p => p.AddUnmatched("class", "form-select").AddUnmatched("aria-label", "Language"));

        var select = cut.Find("select");
        select.ClassName.ShouldBe("form-select");
        select.GetAttribute("aria-label").ShouldBe("Language");
    }

    [Fact]
    public void Change_ShouldChangeLanguage()
    {
        Services.AddTestI18Next();
        var cut = Render<LanguageSelector>();

        cut.Find("select").Change("de");

        I18n.Language.ShouldBe("de");
        cut.Find("select").GetAttribute("value").ShouldBe("de");
        JSInterop.Invocations.Select(i => i.Identifier).ShouldContain("setLanguage");
    }

    [Fact]
    public void Change_EmptyValue_ShouldKeepLanguage()
    {
        Services.AddTestI18Next();
        var cut = Render<LanguageSelector>();

        cut.Find("select").Change(string.Empty);

        I18n.Language.ShouldBe("en");
    }

    [Fact]
    public async Task Render_LanguageChangedElsewhere_ShouldSelectLanguage()
    {
        Services.AddTestI18Next();
        var cut = Render<LanguageSelector>();

        await cut.InvokeAsync(() => I18n.ChangeLanguageAsync("de"));

        cut.Find("select").GetAttribute("value").ShouldBe("de");
    }

    [Fact]
    public void Render_DisplayNameWithLowercaseNativeName_ShouldBeCapitalized()
    {
        Services.AddTestI18Next();

        var cut = Render<LanguageSelector>(p => p.Add(c => c.Languages, new List<string> { "es" }));

        cut.Find("option").TextContent.ShouldBe("Español");
    }
}
