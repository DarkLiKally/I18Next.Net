using System;

#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using I18Next.Net.Logging.InterpolatedStringHandlers;
#endif

namespace I18Next.Net.Logging;

public static class ILoggerExtensions
{
    /// <summary>Formats and writes a critical log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }

    /// <summary>Formats and writes a critical log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Critical, exception, message, args);
    }

    /// <summary>Formats and writes a debug log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    /// <summary>Formats and writes a debug log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }

    /// <summary>Formats and writes an error log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    public static void LogError(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Error, message, args);
    }

    /// <summary>Formats and writes an error log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Error, exception, message, args);
    }

    /// <summary>Formats and writes an informational log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Information, message, args);
    }

    /// <summary>Formats and writes an informational log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Information, exception, message, args);
    }

    /// <summary>Formats and writes a trace log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }

    /// <summary>Formats and writes a trace log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Trace, exception, message, args);
    }

    /// <summary>Formats and writes a warning log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning(this ILogger logger, string message, params object[] args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }

    /// <summary>Formats and writes a warning log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.Log(LogLevel.Warning, exception, message, args);
    }

    #if NET6_0_OR_GREATER
    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        [InterpolatedStringHandlerArgument("logger", "logLevel")]
        ref StructuredLoggingInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(logLevel, template, arguments);
        }
    }

    public static void LogError(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingErrorInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Error, template, arguments);
        }
    }

    public static void LogWarning(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingWarningInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, template, arguments);
        }
    }

    public static void LogInformation(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingInformationInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Information, template, arguments);
        }
    }

    public static void LogDebug(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingDebugInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Debug, template, arguments);
        }
    }

    public static void LogCritical(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingCriticalInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, template, arguments);
        }
    }

    public static void LogTrace(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingTraceInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, template, arguments);
        }
    }

    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger", "logLevel")]
        ref StructuredLoggingInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(logLevel, exception, template, arguments);
        }
    }

    public static void LogError(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingErrorInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Error, exception, template, arguments);
        }
    }

    public static void LogWarning(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingWarningInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, exception, template, arguments);
        }
    }

    public static void LogInformation(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingInformationInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Information, exception, template, arguments);
        }
    }

    public static void LogDebug(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingDebugInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Debug, exception, template, arguments);
        }
    }

    public static void LogCritical(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingCriticalInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, exception, template, arguments);
        }
    }

    public static void LogTrace(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingTraceInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Warning, exception, template, arguments);
        }
    }
    #endif
}
