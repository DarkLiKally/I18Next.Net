using System;
using I18Next.Net.Logging;
using Serilog.Events;
using II18NextLogger = I18Next.Net.Logging.ILogger;
using ILogger = Serilog.ILogger;

namespace I18Next.Net.Serilog;

/// <summary>
///     I18Next logger plugin implementation using Serilog as a target for log messages.
/// </summary>
public class I18NextSerilogLogger : Logging.ILogger
{
    private readonly ILogger _logger;

    /// <summary>
    ///     Default constructor. This will use the globally shared Serilog logger instance.
    /// </summary>
    public I18NextSerilogLogger()
    {
        _logger = global::Serilog.Log.Logger;
    }

    /// <summary>
    ///     Constructor used to provide a specific Serilog logger instance to be used.
    /// </summary>
    /// <param name="logger"></param>
    public I18NextSerilogLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var level = ConvertLogLevel(logLevel);
        return _logger.IsEnabled(level);
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        var level = ConvertLogLevel(logLevel);
        _logger.Write(level, exception, message, args);
    }

    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        var level = ConvertLogLevel(logLevel);
        _logger.Write(level, message, args);
    }

    private LogEventLevel ConvertLogLevel(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:       return LogEventLevel.Verbose;
            case LogLevel.Debug:       return LogEventLevel.Debug;
            case LogLevel.Information: return LogEventLevel.Information;
            case LogLevel.Warning:     return LogEventLevel.Warning;
            case LogLevel.Error:       return LogEventLevel.Error;
            case LogLevel.Critical:    return LogEventLevel.Fatal;
            default:                   return LogEventLevel.Fatal;
        }
    }
}
