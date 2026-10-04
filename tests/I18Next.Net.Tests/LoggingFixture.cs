using System;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using I18Next.Net.Extensions;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests;

[TestFixture]
public class LoggingFixture
{
    private ILogger _logger;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Test]
    public void Extensions_WithoutException_ShouldForwardLogLevel()
    {
        _logger.LogTrace("trace {a}", 1);
        _logger.LogDebug("debug {a}", 1);
        _logger.LogInformation("information {a}", 1);
        _logger.LogWarning("warning {a}", 1);
        _logger.LogError("error {a}", 1);
        _logger.LogCritical("critical {a}", 1);

        _logger.Received(1).Log(LogLevel.Trace, "trace {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Debug, "debug {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Information, "information {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Warning, "warning {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Error, "error {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Critical, "critical {a}", Arg.Any<object[]>());
    }

    [Test]
    public void Extensions_WithException_ShouldForwardLogLevelAndException()
    {
        var exception = new InvalidOperationException();

        _logger.LogTrace(exception, "trace {a}", 1);
        _logger.LogDebug(exception, "debug {a}", 1);
        _logger.LogInformation(exception, "information {a}", 1);
        _logger.LogWarning(exception, "warning {a}", 1);
        _logger.LogError(exception, "error {a}", 1);
        _logger.LogCritical(exception, "critical {a}", 1);

        _logger.Received(1).Log(LogLevel.Trace, exception, "trace {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Debug, exception, "debug {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Information, exception, "information {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Warning, exception, "warning {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Error, exception, "error {a}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Critical, exception, "critical {a}", Arg.Any<object[]>());
    }

#if NET6_0_OR_GREATER
    [Test]
    public void Extensions_InterpolatedStrings_ShouldCreateStructuredTemplates()
    {
        var value = 42;
        var exception = new InvalidOperationException();

        _logger.LogTrace($"trace {value}");
        _logger.LogDebug($"debug {value}");
        _logger.LogInformation($"information {value}");
        _logger.LogWarning($"warning {value}");
        _logger.LogError($"error {value} {{literal}}");
        _logger.LogCritical($"critical {value}");
        ILoggerExtensions.Log(_logger, LogLevel.Information, $"log {value}");
        _logger.LogTrace(exception, $"trace {value}");
        _logger.LogDebug(exception, $"debug {value}");
        _logger.LogInformation(exception, $"information {value}");
        _logger.LogWarning(exception, $"warning {value}");
        _logger.LogError(exception, $"error {value}");
        _logger.LogCritical(exception, $"critical {value}");

        _logger.Received(1).Log(LogLevel.Trace, "trace {@value}", Arg.Is<object[]>(a => (int) a[0] == 42));
        _logger.Received(1).Log(LogLevel.Debug, "debug {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Information, "information {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Warning, "warning {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Error, "error {@value} {{literal}}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Critical, "critical {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Information, "log {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Trace, exception, "trace {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Debug, exception, "debug {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Information, exception, "information {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Warning, exception, "warning {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Error, exception, "error {@value}", Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Critical, exception, "critical {@value}", Arg.Any<object[]>());
    }

    [Test]
    public void Extensions_InterpolatedStringsWithDisabledLevel_ShouldNotLog()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(false);

        logger.LogDebug($"debug {42}");

        logger.DidNotReceiveWithAnyArgs().Log(default, default(string), default);
    }
#endif

    [Test]
    public void TraceLogger_ShouldOnlyWriteEnabledLevels()
    {
        var writer = new StringWriter();
        var listener = new TextWriterTraceListener(writer);
        Trace.Listeners.Add(listener);

        try
        {
            var logger = new TraceLogger { LogLevel = LogLevel.Information };

            logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(LogLevel.Information).Should().BeTrue();

            logger.LogDebug("hidden {value}", 1);
            logger.LogInformation("Value {value,5:D3} and {other}", 7, "text");
            logger.LogWarning("warning {{escaped}} {value}", 2);
            logger.LogError(new InvalidOperationException("{braces}"), "error {value}", 3);
            logger.LogCritical("critical");
            logger.Log(LogLevel.None, "none");
            listener.Flush();

            var output = writer.ToString();

            output.Should().NotContain("hidden");
            output.Should().Contain("Value   007 and text");
            output.Should().Contain("warning {escaped} 2");
            output.Should().Contain("error 3");
            output.Should().Contain("{braces}");
            output.Should().Contain("critical");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    [Test]
    public void DefaultExtensionsLogger_ShouldForwardToMicrosoftLogger()
    {
        var msLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();
        msLogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning).Returns(true);

        var logger = new DefaultExtensionsLogger(msLogger);

        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();

        logger.Log(LogLevel.Warning, "message {a}", 1);
        logger.Log(LogLevel.Error, new InvalidOperationException(), "message {a}", 1);

        msLogger.ReceivedWithAnyArgs(2).Log<object>(default, default, default, default, default);
    }
}
