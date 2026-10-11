using System;

#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using I18Next.Net.Logging.InterpolatedStringHandlers;
#endif

namespace I18Next.Net.Logging;

/// <summary>
///     Extension methods writing log messages of a specific level to an <see cref="ILogger" />. On .NET 6 and later,
///     interpolated strings are written as structured messages whose placeholders are named after the interpolated
///     expressions.
/// </summary>
public static class ILoggerExtensions
{
    /// <summary>Formats and writes a critical log message.</summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical("Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogDebug("Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogError("Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogInformation("Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogTrace("Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
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
    /// <example>logger.LogWarning("Error while processing request from {Address}", address)</example>
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
    /// <summary>
    ///     Formats and writes a log message with the given level from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="logLevel">The severity of the message to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.Log(LogLevel.Warning, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes an error log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogError($"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a warning log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogWarning($"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes an informational log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogInformation($"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a debug log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogDebug($"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a critical log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogCritical($"Loading the namespace {ns} failed")</example>
    public static void LogCritical(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingCriticalInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Critical, template, arguments);
        }
    }

    /// <summary>
    ///     Formats and writes a trace log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogTrace($"Loading the namespace {ns} failed")</example>
    public static void LogTrace(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingTraceInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Trace, template, arguments);
        }
    }

    /// <summary>
    ///     Formats and writes a log message with the given level from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="logLevel">The severity of the message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.Log(LogLevel.Warning, exception, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes an error log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogError(exception, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a warning log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogWarning(exception, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes an informational log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogInformation(exception, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a debug log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogDebug(exception, $"Loading the namespace {ns} failed")</example>
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

    /// <summary>
    ///     Formats and writes a critical log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogCritical(exception, $"Loading the namespace {ns} failed")</example>
    public static void LogCritical(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingCriticalInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Critical, exception, template, arguments);
        }
    }

    /// <summary>
    ///     Formats and writes a trace log message from an interpolated string.
    ///     The interpolated values are only formatted when the level is enabled and are logged as structured
    ///     properties named after their expressions.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="handler">The interpolated message, e.g. <c>$"Loaded {ns} for {language}"</c>.</param>
    /// <example>logger.LogTrace(exception, $"Loading the namespace {ns} failed")</example>
    public static void LogTrace(
        this ILogger logger,
        Exception exception,
        [InterpolatedStringHandlerArgument("logger")]
        ref StructuredLoggingTraceInterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
        {
            var (template, arguments) = handler.GetTemplateAndArguments();
            logger.Log(LogLevel.Trace, exception, template, arguments);
        }
    }
#endif
}
