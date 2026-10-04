using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests;

public class I18NextFixture
{
    public I18NextFixture()
    {
        SetupBackend();

        var translator = new DefaultTranslator(_backend);
        _i18Next = new I18NextNet(_backend, translator);
    }

    private InMemoryBackend _backend;
    private readonly I18NextNet _i18Next;

    private void SetupBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
        backend.AddTranslation("en", "translation", "exampleKey2_plural", "My English plural fallback {{count}}.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");

        _backend = backend;
    }

    [Fact]
    public void English()
    {
        _i18Next.Language = "en";
        _i18Next.T("exampleKey").ShouldBe("My English text.");
    }

    [Fact]
    public void FallbackLanguageIsSet_MissingTranslation_ReturnsFallback()
    {
        _i18Next.Language = "de";
        _i18Next.SetFallbackLanguages("en");
        _i18Next.T("exampleKey2").ShouldBe("My English fallback.");
    }

    [Fact]
    public void German()
    {
        _i18Next.Language = "de";
        _i18Next.T("exampleKey").ShouldBe("Mein deutscher text.");
    }

    [Fact]
    public void MissingLanguage_ReturnsFallback()
    {
        _i18Next.Language = "jp";
        _i18Next.SetFallbackLanguages("en");
        _i18Next.T("exampleKey2").ShouldBe("My English fallback.");
    }

    [Fact]
    public void Pluralization_MissingLanguage_ReturnsFallback()
    {
        _i18Next.Language = "ja";
        _i18Next.SetFallbackLanguages("en");
        _i18Next.T("exampleKey2", new { count = 2 }).ShouldBe("My English plural fallback 2.");
    }

    [Fact]
    public void MissingNamespace_ReturnsFallback()
    {
        _i18Next.Language = "en";
        _i18Next.SetFallbackNamespaces("translation");
        _i18Next.T("translation2:exampleKey2", new { count = 2 }).ShouldBe("My English plural fallback 2.");
    }

    [Fact]
    public void NoFallbackLanguage_MissingTranslation_ReturnsKey()
    {
        _i18Next.Language = "de";
        _i18Next.T("exampleKey2").ShouldBe("exampleKey2");
    }
}
