using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests;

[TestFixture]
public class I18NextFixture
{
    [SetUp]
    public void Setup()
    {
        SetupBackend();

        var translator = new DefaultTranslator(_backend);
        _i18Next = new I18NextNet(_backend, translator);
    }

    private InMemoryBackend _backend;
    private I18NextNet _i18Next;

    private void SetupBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
        backend.AddTranslation("en", "translation", "exampleKey2_plural", "My English plural fallback {{count}}.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");

        _backend = backend;
    }

    [Test]
    public void English()
    {
        _i18Next.Language = "en";
        Assert.AreEqual("My English text.", _i18Next.T("exampleKey"));
    }

    [Test]
    public void FallbackLanguageIsSet_MissingTranslation_ReturnsFallback()
    {
        _i18Next.Language = "de";
        _i18Next.SetFallbackLanguages("en");
        Assert.AreEqual("My English fallback.", _i18Next.T("exampleKey2"));
    }

    [Test]
    public void German()
    {
        _i18Next.Language = "de";
        Assert.AreEqual("Mein deutscher text.", _i18Next.T("exampleKey"));
    }

    [Test]
    public void MissingLanguage_ReturnsFallback()
    {
        _i18Next.Language = "jp";
        _i18Next.SetFallbackLanguages("en");
        Assert.AreEqual("My English fallback.", _i18Next.T("exampleKey2"));
    }

    [Test]
    public void Pluralization_MissingLanguage_ReturnsFallback()
    {
        _i18Next.Language = "ja";
        _i18Next.SetFallbackLanguages("en");
        Assert.AreEqual("My English plural fallback 2.", _i18Next.T("exampleKey2", new { count = 2 }));
    }

    [Test]
    public void NoFallbackLanguage_MissingTranslation_ReturnsKey()
    {
        _i18Next.Language = "de";
        Assert.AreEqual("exampleKey2", _i18Next.T("exampleKey2"));
    }
}