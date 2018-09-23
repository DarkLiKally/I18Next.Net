using System;

namespace I18Next.Net.Plugins
{
    /// <summary>ILogger interface for common scenarios.</summary>
    public interface ILogger
    {
        /// <summary>Formats and writes a critical log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        void LogCritical(string message, params object[] args);

        /// <summary>Formats and writes a critical log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        void LogCritical(Exception exception, string message, params object[] args);

        /// <summary>Formats and writes a debug log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        void LogDebug(string message, params object[] args);

        /// <summary>Formats and writes a debug log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        void LogDebug(Exception exception, string message, params object[] args);

        /// <summary>Formats and writes an error log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        void LogError(string message, params object[] args);

        /// <summary>Formats and writes an error log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        void LogError(Exception exception, string message, params object[] args);

        /// <summary>Formats and writes an informational log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        void LogInformation(string message, params object[] args);

        /// <summary>Formats and writes an informational log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        void LogInformation(Exception exception, string message, params object[] args);

        /// <summary>Formats and writes a trace log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        void LogTrace(string message, params object[] args);

        /// <summary>Formats and writes a trace log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        void LogTrace(Exception exception, string message, params object[] args);

        /// <summary>Formats and writes a warning log message.</summary>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        void LogWarning(string message, params object[] args);

        /// <summary>Formats and writes a warning log message.</summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">
        ///     Format string of the log message in message template format. Example:
        ///     <code>"User {User} logged in from {Address}"</code>
        /// </param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        void LogWarning(Exception exception, string message, params object[] args);
    }
}
