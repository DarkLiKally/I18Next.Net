using System;

using I18Next.Net.Formatters;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class DefaultFormatterFixture
{
    public DefaultFormatterFixture()
    {
        _logger = Substitute.For<ILogger>();
        _formatter = new DefaultFormatter(_logger);
    }
    private readonly ILogger _logger;
    private readonly DefaultFormatter _formatter;


    [Fact]
    public void Format_ValidFormat_ShouldUseCulture()
    {
        _formatter.CanFormat(1, "N2", "de-DE").ShouldBeTrue();
        _formatter.Format(1234.5, "N2", "de-DE").ShouldBe("1.234,50");
        _formatter.Format(1234.5, "N2", "en-US").ShouldBe("1,234.50");
    }

    [Fact]
    public void Format_UppercaseAndLowercase_ShouldUseCulture()
    {
        _formatter.Format("istanbul", "uppercase", "tr-TR").ShouldBe("İSTANBUL");
        _formatter.Format("istanbul", "UPPERCASE", "en").ShouldBe("ISTANBUL");
        _formatter.Format("ÄBC", "lowercase", "de").ShouldBe("äbc");
        _formatter.Format("ABC", "lowercase", "not-a-culture-xx").ShouldBe("abc");
    }

    [Fact]
    public void Format_NullValueOrFormat_ShouldHandleGracefully()
    {
        _formatter.Format(null, "N2", "en-US").ShouldBeNull();
        _formatter.Format(12, null, "en-US").ShouldBe("12");
    }

    [Fact]
    public void Format_UnknownCulture_ShouldUseInvariantCultureAndLog()
    {
        _formatter.Format(1234.5, "N1", "invalid culture!").ShouldBe("1,234.5");

        _logger.Received(1).Log(LogLevel.Information, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Is<object[]>(a => a.Length == 1));
    }

    [Fact]
    public void Format_InvalidFormat_ShouldReturnValueAndLog()
    {
        _formatter.Format(12, "{", "en-US").ShouldBe("12");

        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<FormatException>(), Arg.Any<string>(), Arg.Is<object[]>(a => (string)a[0] == "{"));
    }

    [Fact]
    public void Format_InvalidFormatWithTraceLogger_ShouldNotThrow()
    {
        var formatter = new DefaultFormatter(new TraceLogger());

        formatter.Format(12, "{", "en-US").ShouldBe("12");
    }

    [Fact]
    public void LowercaseAndUppercaseFormatter_NullValue_ShouldReturnNull()
    {
        new LowercaseFormatter().Format(null, "lowercase", "en").ShouldBeNull();
        new UppercaseFormatter().Format(null, "uppercase", "en").ShouldBeNull();
    }
}
