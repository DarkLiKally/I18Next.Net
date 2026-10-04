using System;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests;

public class I18NextLanguageDetectionFixture
{
    public I18NextLanguageDetectionFixture()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");
        backend.AddTranslation("de", "other", "exampleKey", "Mein anderer text.");

        _languageDetector = new TestLanguageDetector { Language = "de" };
        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend), _languageDetector) { Language = "en" };
    }
    private I18NextNet _i18Next;
    private TestLanguageDetector _languageDetector;


    [Fact]
    public void T_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        _i18Next.T("exampleKey").ShouldBe("Mein deutscher text.");

        _languageDetector.Language = "en";

        _i18Next.T("exampleKey").ShouldBe("My English text.");
    }

    [Fact]
    public async Task Ta_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        (await _i18Next.Ta("exampleKey")).ShouldBe("Mein deutscher text.");
    }

    [Fact]
    public void T_DetectLanguageOnEachTranslation_ShouldNotChangeLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        _i18Next.T("exampleKey");

        _i18Next.Language.ShouldBe("en");
    }

    [Fact]
    public void T_DetectorReturnsNothing_ShouldUseLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;
        _languageDetector.Language = null;

        _i18Next.T("exampleKey").ShouldBe("My English text.");
    }

    [Fact]
    public void T_ExplicitLanguage_ShouldNotBeOverriddenByDetection()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;
        _languageDetector.Language = "de";

        _i18Next.T("en", "exampleKey").ShouldBe("My English text.");
    }

    [Fact]
    public void T_ExplicitNamespace_ShouldUseNamespace()
    {
        _i18Next.T("de", "other", "exampleKey").ShouldBe("Mein anderer text.");
    }

    [Fact]
    public void T_DetectionDisabled_ShouldUseLanguage()
    {
        _i18Next.T("exampleKey").ShouldBe("My English text.");
    }

    [Fact]
    public void UseDetectedLanguage_ShouldChangeLanguageAndRaiseEvent()
    {
        LanguageChangedEventArgs eventArgs = null;
        _i18Next.LanguageChanged += (_, args) => eventArgs = args;

        _i18Next.UseDetectedLanguage();

        _i18Next.Language.ShouldBe("de");
        eventArgs.ShouldNotBeNull();
        eventArgs.OldLanguage.ShouldBe("en");
        eventArgs.NewLanguage.ShouldBe("de");
    }

    [Fact]
    public void Language_SetToSameValue_ShouldNotRaiseEvent()
    {
        var raised = false;
        _i18Next.LanguageChanged += (_, _) => raised = true;

        _i18Next.Language = "en";

        raised.ShouldBeFalse();
    }

    [Fact]
    public void InvalidSettings_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _i18Next.Language = " ");
        Should.Throw<ArgumentNullException>(() => _i18Next.DefaultNamespace = "");
        Should.Throw<ArgumentNullException>(() => _i18Next.FallbackLanguages = null);
        Should.Throw<ArgumentNullException>(() => new I18NextNet(null, new DefaultTranslator(new InMemoryBackend())));
    }

    private class TestLanguageDetector : ILanguageDetector
    {
        public string Language { get; set; }

        public string GetLanguage()
        {
            return Language;
        }
    }
}
