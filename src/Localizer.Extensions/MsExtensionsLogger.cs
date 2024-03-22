using System;
using Microsoft.Extensions.Logging;
using LogLevel = Localizer.Logging.LogLevel;

namespace Localizer.Extensions;

/// <summary>
///     Logger implementation which forwards the logging method calls to a Microsoft.Extensions.Logging.ILogger.
/// </summary>
public class DefaultExtensionsLogger : Logging.ILogger
{
    private readonly ILogger _logger;

    public DefaultExtensionsLogger(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel)(int)logLevel);
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        var extensionsLogLevel = (Microsoft.Extensions.Logging.LogLevel)(int)logLevel;
        _logger.Log(extensionsLogLevel, exception, message, args);
    }

    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        var extensionsLogLevel = (Microsoft.Extensions.Logging.LogLevel)(int)logLevel;
        _logger.Log(extensionsLogLevel, message, args);
    }
}
