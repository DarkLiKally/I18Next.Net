using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultLanguageDetectorFixture
{
    [Fact]
    public void GetLanguage_ShouldReturnProvidedLanguage()
    {
        var detector = new DefaultLanguageDetector("de-DE");
        detector.GetLanguage().ShouldBe("de-DE");

        detector = new DefaultLanguageDetector("en-US");
        detector.GetLanguage().ShouldBe("en-US");

        detector = new DefaultLanguageDetector("fr");
        detector.GetLanguage().ShouldBe("fr");
    }
}
