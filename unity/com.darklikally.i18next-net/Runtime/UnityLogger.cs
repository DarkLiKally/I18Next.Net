using System;
using System.Text;

using I18Next.Net.Logging;

using UnityEngine;

using ILogger = I18Next.Net.Logging.ILogger;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     Logger implementation that writes the log messages to the Unity console.
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        ///     The minimum level of the logged messages. Defaults to <see cref="Logging.LogLevel.Warning" />.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && (int)LogLevel <= (int)logLevel;
        }

        /// <summary>
        ///     Writes the message to the Unity console like <see cref="Log(LogLevel, string, object[])" /> and the exception
        ///     with <see cref="Debug.LogException(Exception)" />.
        /// </summary>
        /// <param name="logLevel">The severity of the message.</param>
        /// <param name="exception">The exception to log or <c>null</c>.</param>
        /// <param name="message">The message template, e.g. <c>Missing key {key}</c>.</param>
        /// <param name="args">The values replacing the placeholders of the template in their order.</param>
        public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {
            if (!IsEnabled(logLevel))
                return;

            Log(logLevel, message, args);

            if (exception != null)
                Debug.LogException(exception);
        }

        /// <summary>
        ///     Writes the message to the Unity console with <see cref="Debug.Log(object)" />,
        ///     <see cref="Debug.LogWarning(object)" /> or <see cref="Debug.LogError(object)" /> depending on the level. The
        ///     placeholders of the template are replaced by the arguments in their order.
        /// </summary>
        /// <param name="logLevel">The severity of the message.</param>
        /// <param name="message">The message template, e.g. <c>Missing key {key}</c>.</param>
        /// <param name="args">The values replacing the placeholders of the template in their order.</param>
        public void Log(LogLevel logLevel, string message, params object[] args)
        {
            if (!IsEnabled(logLevel))
                return;

            var text = Format(message, args);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    Debug.Log(text);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(text);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(text);
                    break;
            }
        }

        internal static string Format(string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            var builder = new StringBuilder(message.Length);
            var argIndex = 0;

            for (var i = 0; i < message.Length; i++)
            {
                var c = message[i];

                if ((c == '{' || c == '}') && i + 1 < message.Length && message[i + 1] == c)
                {
                    builder.Append(c);
                    i++;
                    continue;
                }

                var end = c == '{' ? message.IndexOf('}', i + 1) : -1;

                if (end < 0)
                {
                    builder.Append(c);
                    continue;
                }

                if (args != null && argIndex < args.Length)
                    builder.Append(args[argIndex++]);
                else
                    builder.Append(message, i, end - i + 1);

                i = end;
            }

            return builder.ToString();
        }
    }
}
