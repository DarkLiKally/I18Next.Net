using System.Globalization;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests;

public class I18NextStringLocalizerFixture
{
    private InMemoryBackend _backend;
    private I18NextNet _i18Next;
    private I18NextStringLocalizer _i18NextStringLocalizer;
    
    [SetUp]
    public void Setup()
    {
        SetupBackend();

        var translator = new DefaultTranslator(_backend);
        _i18Next = new I18NextNet(_backend, translator);
        _i18NextStringLocalizer = new I18NextStringLocalizer(_i18Next);
    }
    
    private void SetupBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
        backend.AddTranslation("en", "translation", "exampleParam", "My {{Param}}.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");

        _backend = backend;
    }

    [Test]
    public void English()
    {
        _i18NextStringLocalizer.WithCulture(new CultureInfo("en"));
        _i18NextStringLocalizer["translation:exampleKey"].Value.Should().Be("My English text.");
    }

    [Test]
    public void FallbackLanguageIsSet_MissingTranslation_ReturnsFallback()
    {
        _i18Next.SetFallbackLanguages("en");
        _i18NextStringLocalizer.WithCulture(new CultureInfo("de"));
        _i18NextStringLocalizer["translation:exampleKey2"].Value.Should().Be("My English fallback.");
    }

    [Test]
    public void German()
    {
        _i18NextStringLocalizer.WithCulture(new CultureInfo("de"));
        _i18NextStringLocalizer["translation:exampleKey"].Value.Should().Be("Mein deutscher text.");
    }

    [Test]
    public void Parameter()
    {
        _i18NextStringLocalizer.WithCulture(new CultureInfo("en"));
        _i18NextStringLocalizer["translation:exampleParam", new { Param = "value" }].Value.Should().Be("My value.");
    }

    [Test]
    public void ResourceNotFoundSet()
    {
        _i18NextStringLocalizer.WithCulture(new CultureInfo("en"));
        var result = _i18NextStringLocalizer["missing"];

        result.Value.Should().Be("missing");
        result.ResourceNotFound.Should().Be(true);
        result.Name.Should().Be("missing");
    }
}
