using System;
using Serilog;
using II18NextLogger = I18Next.Net.Plugins.ILogger;

namespace I18Next.Net.Serilog;

/// <summary>
///     I18Next logger plugin implementation using Serilog as a target for log messages.
/// </summary>
public class I18NextSerilogLogger : II18NextLogger
{
    private readonly ILogger _logger;

    /// <summary>
    ///     Default constructor. This will use the globally shared Serilog logger instance.
    /// </summary>
    public I18NextSerilogLogger()
    {
        _logger = Log.Logger;
    }

    /// <summary>
    ///     Constructor used to provide a specific Serilog logger instance to be used.
    /// </summary>
    /// <param name="logger"></param>
    public I18NextSerilogLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogCritical(string message, params object[] args)
    {
        _logger.Fatal(message, args);
    }

    /// <inheritdoc />
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        _logger.Fatal(exception, message, args);
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    /// <inheritdoc />
    public void LogDebug(Exception exception, string message, params object[] args)
    {
        _logger.Debug(exception, message, args);
    }

    /// <inheritdoc />
    public void LogError(string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    /// <inheritdoc />
    public void LogInformation(Exception exception, string message, params object[] args)
    {
        _logger.Information(exception, message, args);
    }

    /// <inheritdoc />
    public void LogVerbose(string message, params object[] args)
    {
        _logger.Verbose(message, args);
    }

    /// <inheritdoc />
    public void LogVerbose(Exception exception, string message, params object[] args)
    {
        _logger.Verbose(exception, message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(Exception exception, string message, params object[] args)
    {
        _logger.Warning(exception, message, args);
    }
}