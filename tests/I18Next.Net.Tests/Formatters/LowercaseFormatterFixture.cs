using FluentAssertions;
using I18Next.Net.Formatters;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters;

[TestFixture]
public class LowercaseFormatterFixture
{
    private LowercaseFormatter _formatter;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _formatter = new LowercaseFormatter();
    }

    [Test]
    public void CanFormat_ProvideOtherFormat_ShouldReturnFalse()
    {
        _formatter.CanFormat("Test", "uppercase", "en-US").Should().BeFalse();
        _formatter.CanFormat("Test", "test", "en-US").Should().BeFalse();
        _formatter.CanFormat("Test", "DD/MM/YYYY", "en-US").Should().BeFalse();
    }

    [Test]
    public void CanFormat_ProvideUppercaseFormat_ShouldReturnTrue()
    {
        _formatter.CanFormat("Test", "lowercase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "LowerCase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "lowerCase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "LoWeRcAsE", "en-US").Should().BeTrue();
    }

    [Test]
    public void Format_ProvideMixedCase_ShouldFormatToUppercase()
    {
        _formatter.Format("test", "uppercase", "en-US").Should().Be("test");
        _formatter.Format("test Test", "uppercase", "en-US").Should().Be("test test");
        _formatter.Format("test test", "uppercase", "en-US").Should().Be("test test");
        _formatter.Format("TEST Test", "uppercase", "en-US").Should().Be("test test");
        _formatter.Format("tEsT TeSt", "uppercase", "en-US").Should().Be("test test");
    }
}