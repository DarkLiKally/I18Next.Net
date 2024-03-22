using System;

namespace Localizer.Logging;

/// <summary>
///     ILogger interface for common scenarios.
/// </summary>
public interface ILogger
{
    /// <summary>Checks whether the given log level is enabled in this logger instance.</summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>Boolean whether the log level is enabled</returns>
    bool IsEnabled(LogLevel logLevel);

    /// <summary>Formats and writes a log message with the given level.</summary>
    /// <param name="logLevel">The severity of the message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Error, exception, "Error while processing request from {Address}", address)</example>
    void Log(LogLevel logLevel, Exception exception, string message, params object[] args);

    /// <summary>Formats and writes a log message with the given level.</summary>
    /// <param name="logLevel">The severity of the message to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <code>"User {User} logged in from {Address}"</code>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Error, exception, "Error while processing request from {Address}", address)</example>
    void Log(LogLevel logLevel, string message, params object[] args);
}
