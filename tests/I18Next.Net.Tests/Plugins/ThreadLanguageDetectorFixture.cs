using System.Globalization;
using FluentAssertions;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class ThreadLanguageDetectorFixture
{
    private CultureInfo _originalCulture;

    [SetUp]
    public void SetUp()
    {
        _originalCulture = CultureInfo.CurrentCulture;
    }

    [TearDown]
    public void TearDown()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [Test]
    public void GetLanguage_ShouldReturnCurrentCulture()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        new ThreadLanguageDetector().GetLanguage().Should().Be("de-DE");
    }

    [Test]
    public void GetLanguage_InvariantCulture_ShouldReturnFallback()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        new ThreadLanguageDetector().GetLanguage().Should().Be("en-US");
        new ThreadLanguageDetector("fr").GetLanguage().Should().Be("fr");
    }
}
