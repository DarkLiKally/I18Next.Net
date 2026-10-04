using System;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests;

[TestFixture]
public class I18NextLanguageDetectionFixture
{
    private I18NextNet _i18Next;
    private TestLanguageDetector _languageDetector;

    [SetUp]
    public void SetUp()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");
        backend.AddTranslation("de", "other", "exampleKey", "Mein anderer text.");

        _languageDetector = new TestLanguageDetector { Language = "de" };
        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend), _languageDetector) { Language = "en" };
    }

    [Test]
    public void T_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        _i18Next.T("exampleKey").Should().Be("Mein deutscher text.");

        _languageDetector.Language = "en";

        _i18Next.T("exampleKey").Should().Be("My English text.");
    }

    [Test]
    public async Task Ta_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        (await _i18Next.Ta("exampleKey")).Should().Be("Mein deutscher text.");
    }

    [Test]
    public void T_DetectLanguageOnEachTranslation_ShouldNotChangeLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;

        _i18Next.T("exampleKey");

        _i18Next.Language.Should().Be("en");
    }

    [Test]
    public void T_DetectorReturnsNothing_ShouldUseLanguage()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;
        _languageDetector.Language = null;

        _i18Next.T("exampleKey").Should().Be("My English text.");
    }

    [Test]
    public void T_ExplicitLanguage_ShouldNotBeOverriddenByDetection()
    {
        _i18Next.DetectLanguageOnEachTranslation = true;
        _languageDetector.Language = "de";

        _i18Next.T("en", "exampleKey").Should().Be("My English text.");
    }

    [Test]
    public void T_ExplicitNamespace_ShouldUseNamespace()
    {
        _i18Next.T("de", "other", "exampleKey").Should().Be("Mein anderer text.");
    }

    [Test]
    public void T_DetectionDisabled_ShouldUseLanguage()
    {
        _i18Next.T("exampleKey").Should().Be("My English text.");
    }

    [Test]
    public void UseDetectedLanguage_ShouldChangeLanguageAndRaiseEvent()
    {
        LanguageChangedEventArgs eventArgs = null;
        _i18Next.LanguageChanged += (_, args) => eventArgs = args;

        _i18Next.UseDetectedLanguage();

        _i18Next.Language.Should().Be("de");
        eventArgs.Should().NotBeNull();
        eventArgs.OldLanguage.Should().Be("en");
        eventArgs.NewLanguage.Should().Be("de");
    }

    [Test]
    public void Language_SetToSameValue_ShouldNotRaiseEvent()
    {
        var raised = false;
        _i18Next.LanguageChanged += (_, _) => raised = true;

        _i18Next.Language = "en";

        raised.Should().BeFalse();
    }

    [Test]
    public void InvalidSettings_ShouldThrow()
    {
        _i18Next.Invoking(i => i.Language = " ").Should().Throw<ArgumentNullException>();
        _i18Next.Invoking(i => i.DefaultNamespace = "").Should().Throw<ArgumentNullException>();
        _i18Next.Invoking(i => i.FallbackLanguages = null).Should().Throw<ArgumentNullException>();
        this.Invoking(_ => new I18NextNet(null, new DefaultTranslator(new InMemoryBackend()))).Should().Throw<ArgumentNullException>();
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
