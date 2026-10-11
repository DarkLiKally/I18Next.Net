using System;

using Microsoft.Extensions.Logging;

using LogLevel = I18Next.Net.Logging.LogLevel;

namespace I18Next.Net.Extensions;

/// <summary>
///     Logger implementation which forwards the logging method calls to a Microsoft.Extensions.Logging.ILogger.
/// </summary>
/// <param name="logger">The logger the messages are written to.</param>
public class DefaultExtensionsLogger(ILogger logger) : Logging.ILogger
{
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel)(int)logLevel);
    }

    /// <inheritdoc />
    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        var extensionsLogLevel = (Microsoft.Extensions.Logging.LogLevel)(int)logLevel;
        _logger.Log(extensionsLogLevel, exception, message, args);
    }

    /// <inheritdoc />
    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        var extensionsLogLevel = (Microsoft.Extensions.Logging.LogLevel)(int)logLevel;
        _logger.Log(extensionsLogLevel, message, args);
    }
}
