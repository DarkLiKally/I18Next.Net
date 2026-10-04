using System;
using FluentAssertions;
using I18Next.Net.Formatters;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters;

[TestFixture]
public class DefaultFormatterFixture
{
    private ILogger _logger;
    private DefaultFormatter _formatter;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger>();
        _formatter = new DefaultFormatter(_logger);
    }

    [Test]
    public void Format_ValidFormat_ShouldUseCulture()
    {
        _formatter.CanFormat(1, "N2", "de-DE").Should().BeTrue();
        _formatter.Format(1234.5, "N2", "de-DE").Should().Be("1.234,50");
        _formatter.Format(1234.5, "N2", "en-US").Should().Be("1,234.50");
    }

    [Test]
    public void Format_NullValueOrFormat_ShouldHandleGracefully()
    {
        _formatter.Format(null, "N2", "en-US").Should().BeNull();
        _formatter.Format(12, null, "en-US").Should().Be("12");
    }

    [Test]
    public void Format_UnknownCulture_ShouldUseInvariantCultureAndLog()
    {
        _formatter.Format(1234.5, "N1", "invalid culture!").Should().Be("1,234.5");

        _logger.Received(1).Log(LogLevel.Information, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Is<object[]>(a => a.Length == 1));
    }

    [Test]
    public void Format_InvalidFormat_ShouldReturnValueAndLog()
    {
        _formatter.Format(12, "{", "en-US").Should().Be("12");

        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<FormatException>(), Arg.Any<string>(), Arg.Is<object[]>(a => (string) a[0] == "{"));
    }

    [Test]
    public void Format_InvalidFormatWithTraceLogger_ShouldNotThrow()
    {
        var formatter = new DefaultFormatter(new TraceLogger());

        formatter.Format(12, "{", "en-US").Should().Be("12");
    }

    [Test]
    public void LowercaseAndUppercaseFormatter_NullValue_ShouldReturnNull()
    {
        new LowercaseFormatter().Format(null, "lowercase", "en").Should().BeNull();
        new UppercaseFormatter().Format(null, "uppercase", "en").Should().BeNull();
    }
}
