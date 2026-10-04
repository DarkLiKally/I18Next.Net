using I18Next.Net.Formatters;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class LowercaseFormatterFixture
{
    public LowercaseFormatterFixture()
    {
        _formatter = new LowercaseFormatter();
    }
    private LowercaseFormatter _formatter;


    [Fact]
    public void CanFormat_ProvideOtherFormat_ShouldReturnFalse()
    {
        _formatter.CanFormat("Test", "uppercase", "en-US").ShouldBeFalse();
        _formatter.CanFormat("Test", "test", "en-US").ShouldBeFalse();
        _formatter.CanFormat("Test", "DD/MM/YYYY", "en-US").ShouldBeFalse();
    }

    [Fact]
    public void CanFormat_ProvideUppercaseFormat_ShouldReturnTrue()
    {
        _formatter.CanFormat("Test", "lowercase", "en-US").ShouldBeTrue();
        _formatter.CanFormat("Test", "LowerCase", "en-US").ShouldBeTrue();
        _formatter.CanFormat("Test", "lowerCase", "en-US").ShouldBeTrue();
        _formatter.CanFormat("Test", "LoWeRcAsE", "en-US").ShouldBeTrue();
    }

    [Fact]
    public void Format_ProvideMixedCase_ShouldFormatToUppercase()
    {
        _formatter.Format("test", "uppercase", "en-US").ShouldBe("test");
        _formatter.Format("test Test", "uppercase", "en-US").ShouldBe("test test");
        _formatter.Format("test test", "uppercase", "en-US").ShouldBe("test test");
        _formatter.Format("TEST Test", "uppercase", "en-US").ShouldBe("test test");
        _formatter.Format("tEsT TeSt", "uppercase", "en-US").ShouldBe("test test");
    }
}