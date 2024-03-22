using FluentAssertions;
using Localizer.Formatters;
using NUnit.Framework;

namespace Localizer.Tests.Formatters;

[TestFixture]
public class UppercaseFormatterFixture
{
    private UppercaseFormatter _formatter;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _formatter = new UppercaseFormatter();
    }

    [Test]
    public void CanFormat_ProvideOtherFormat_ShouldReturnFalse()
    {
        _formatter.CanFormat("Test", "lowercase", "en-US").Should().BeFalse();
        _formatter.CanFormat("Test", "test", "en-US").Should().BeFalse();
        _formatter.CanFormat("Test", "DD/MM/YYYY", "en-US").Should().BeFalse();
    }

    [Test]
    public void CanFormat_ProvideUppercaseFormat_ShouldReturnTrue()
    {
        _formatter.CanFormat("Test", "uppercase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "UpperCase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "upperCase", "en-US").Should().BeTrue();
        _formatter.CanFormat("Test", "UpPeRcAsE", "en-US").Should().BeTrue();
    }

    [Test]
    public void Format_ProvideMixedCase_ShouldFormatToUppercase()
    {
        _formatter.Format("test", "uppercase", "en-US").Should().Be("TEST");
        _formatter.Format("test Test", "uppercase", "en-US").Should().Be("TEST TEST");
        _formatter.Format("test test", "uppercase", "en-US").Should().Be("TEST TEST");
        _formatter.Format("TEST Test", "uppercase", "en-US").Should().Be("TEST TEST");
        _formatter.Format("tEsT TeSt", "uppercase", "en-US").Should().Be("TEST TEST");
    }
}