using System;

using I18Next.Net.Logging;

using Serilog.Events;

using II18NextLogger = I18Next.Net.Logging.ILogger;
using ILogger = Serilog.ILogger;

namespace I18Next.Net.Serilog;

/// <summary>
///     I18Next logger plugin implementation using Serilog as a target for log messages. <see cref="LogLevel.Trace" /> is
///     written as <see cref="LogEventLevel.Verbose" /> and <see cref="LogLevel.Critical" /> as
///     <see cref="LogEventLevel.Fatal" />.
/// </summary>
public class I18NextSerilogLogger : Logging.ILogger
{
    private readonly ILogger _logger;

    /// <summary>
    ///     Creates a logger writing to the globally shared Serilog logger <c>Log.Logger</c> as configured at creation.
    /// </summary>
    public I18NextSerilogLogger()
    {
        _logger = global::Serilog.Log.Logger;
    }

    /// <summary>
    ///     Creates a logger writing to the given Serilog logger.
    /// </summary>
    /// <param name="logger">The Serilog logger the messages are written to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger" /> is <c>null</c>.</exception>
    public I18NextSerilogLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        var level = ConvertLogLevel(logLevel);
        return _logger.IsEnabled(level);
    }

    /// <inheritdoc />
    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        var level = ConvertLogLevel(logLevel);
        _logger.Write(level, exception, message, args);
    }

    /// <inheritdoc />
    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        var level = ConvertLogLevel(logLevel);
        _logger.Write(level, message, args);
    }

    private LogEventLevel ConvertLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Fatal,
        };
    }
}
