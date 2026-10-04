using System;
using System.Globalization;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class ThreadLanguageDetectorFixture : IDisposable
{
    public ThreadLanguageDetectorFixture()
    {
        _originalCulture = CultureInfo.CurrentCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    
    }
    private CultureInfo _originalCulture;



    [Fact]
    public void GetLanguage_ShouldReturnCurrentCulture()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        new ThreadLanguageDetector().GetLanguage().ShouldBe("de-DE");
    }

    [Fact]
    public void GetLanguage_InvariantCulture_ShouldReturnFallback()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        new ThreadLanguageDetector().GetLanguage().ShouldBe("en-US");
        new ThreadLanguageDetector("fr").GetLanguage().ShouldBe("fr");
    }
}
