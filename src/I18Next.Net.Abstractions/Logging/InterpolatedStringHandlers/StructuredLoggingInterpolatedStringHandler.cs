#if NET6_0_OR_GREATER
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace I18Next.Net.Logging.InterpolatedStringHandlers;

/// <summary>
///     Builds a message template and its arguments from an interpolated string for the structured logging methods of
///     <see cref="ILoggerExtensions" />. Braces in the literal parts are escaped and each interpolated value becomes a
///     placeholder named after its expression, e.g. <c>{@count}</c>. Created by the compiler.
/// </summary>
[InterpolatedStringHandler]
public ref struct StructuredLoggingInterpolatedStringHandler
{
    private readonly StringBuilder _template = null!;
    private readonly List<object?> _arguments = null!;

    /// <summary>
    ///     Whether the log level is enabled. Nothing is collected when it is disabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     Creates a handler for an interpolated string logged with the given level.
    /// </summary>
    /// <param name="literalLength">The number of literal characters in the interpolated string.</param>
    /// <param name="formattedCount">The number of interpolated values.</param>
    /// <param name="logger">The logger the message is written to.</param>
    /// <param name="logLevel">The level of the message.</param>
    /// <param name="isEnabled">Whether the level is enabled. When <c>false</c>, the parts are not appended.</param>
    public StructuredLoggingInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        ILogger logger,
        LogLevel logLevel,
        out bool isEnabled)
    {
        IsEnabled = isEnabled = logger.IsEnabled(logLevel);
        if (isEnabled)
        {
            _template = new StringBuilder(literalLength);
            _arguments = new List<object?>(formattedCount);
        }
    }

    /// <summary>
    ///     Appends a literal part of the interpolated string with escaped braces.
    /// </summary>
    /// <param name="s">The literal part.</param>
    public void AppendLiteral(string s)
    {
        if (!IsEnabled)
            return;

        _template.Append(s.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Appends an interpolated value as placeholder named after its expression.
    /// </summary>
    /// <param name="value">The interpolated value.</param>
    /// <param name="name">The expression of the value, provided by the compiler.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    public void AppendFormatted<T>(
        T value,
        [CallerArgumentExpression("value")] string name = "")
    {
        if (!IsEnabled)
            return;

        _arguments.Add(value);
        _template.Append($"{{@{name}}}");
    }

    /// <summary>
    ///     Gets the collected message template and its arguments.
    /// </summary>
    /// <returns>The message template and the interpolated values in the order of their placeholders.</returns>
    public (string, object?[]) GetTemplateAndArguments() => (_template.ToString(), _arguments.ToArray());
}

#endif
