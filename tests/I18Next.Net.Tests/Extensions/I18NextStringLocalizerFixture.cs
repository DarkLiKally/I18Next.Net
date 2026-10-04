using System.Globalization;
using System.Linq;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Extensions;

public class I18NextStringLocalizerFixture
{
    public I18NextStringLocalizerFixture()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
        backend.AddTranslation("en", "translation", "exampleParam", "My {{Param}}.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");
        backend.AddTranslation("de", "other", "exampleKey", "Mein anderer text.");

        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };
        _localizer = new I18NextStringLocalizer(_i18Next);
    }
    private I18NextNet _i18Next;
    private I18NextStringLocalizer _localizer;


    [Fact]
    public void Indexer_English_ShouldTranslate()
    {
        _localizer.WithCulture(new CultureInfo("en"));

        _localizer["translation:exampleKey"].Value.ShouldBe("My English text.");
    }

    [Fact]
    public void Indexer_German_ShouldTranslate()
    {
        _localizer.WithCulture(new CultureInfo("de"));

        _localizer["exampleKey"].Value.ShouldBe("Mein deutscher text.");
    }

    [Fact]
    public void Indexer_FallbackLanguage_ShouldReturnFallback()
    {
        _i18Next.SetFallbackLanguages("en");
        _localizer.WithCulture(new CultureInfo("de"));

        _localizer["translation:exampleKey2"].Value.ShouldBe("My English fallback.");
    }

    [Fact]
    public void Indexer_WithArguments_ShouldInterpolate()
    {
        _localizer["exampleParam", new { Param = "value" }].Value.ShouldBe("My value.");
    }

    [Fact]
    public void Indexer_ExistingTranslation_ShouldNotSetResourceNotFound()
    {
        var result = _localizer["exampleKey"];

        result.Name.ShouldBe("exampleKey");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_MissingTranslation_ShouldSetResourceNotFound()
    {
        var result = _localizer["missing"];

        result.Value.ShouldBe("missing");
        result.Name.ShouldBe("missing");
        result.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void Indexer_MissingTranslationWithNamespace_ShouldSetResourceNotFound()
    {
        var result = _localizer["translation:missing"];

        result.Value.ShouldBe("missing");
        result.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void Indexer_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        var backend = (InMemoryBackend) _i18Next.Backend;
        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend), new DefaultLanguageDetector("de"))
        {
            Language = "en",
            DetectLanguageOnEachTranslation = true
        };

        new I18NextStringLocalizer(i18Next)["exampleKey"].Value.ShouldBe("Mein deutscher text.");
        i18Next.Language.ShouldBe("en");
    }

    [Fact]
    public void Constructor_WithNamespace_ShouldUseNamespace()
    {
        var localizer = new I18NextStringLocalizer(_i18Next, "other");
        localizer.WithCulture(new CultureInfo("de"));

        localizer["exampleKey"].Value.ShouldBe("Mein anderer text.");
        localizer.GetAllStrings(false).Select(s => s.Value).ShouldBe(new[] { "Mein anderer text." });
    }

    [Fact]
    public void GetAllStrings_ShouldReturnAllTranslations()
    {
        var strings = _localizer.GetAllStrings(false).ToDictionary(s => s.Name, s => s.Value);

        strings.Count().ShouldBe(3);
        strings["exampleKey"].ShouldBe("My English text.");
    }

    [Fact]
    public void GetAllStrings_MissingNamespace_ShouldReturnEmpty()
    {
        _localizer.WithCulture(new CultureInfo("fr"));

        _localizer.GetAllStrings(false).ShouldBeEmpty();
    }
}
