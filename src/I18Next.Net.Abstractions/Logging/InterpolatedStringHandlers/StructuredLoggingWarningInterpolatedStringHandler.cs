#if NET6_0_OR_GREATER
#nullable enable
using System.Runtime.CompilerServices;

namespace I18Next.Net.Logging.InterpolatedStringHandlers;

/// <summary>
///     Builds a structured message template and its arguments from an interpolated string for the <c>LogWarning</c>
///     methods of <see cref="ILoggerExtensions" />, like <see cref="StructuredLoggingInterpolatedStringHandler" /> with
///     the level <see cref="LogLevel.Warning" />. Created by the compiler.
/// </summary>
[InterpolatedStringHandler]
public ref struct StructuredLoggingWarningInterpolatedStringHandler
{
    private readonly StructuredLoggingInterpolatedStringHandler _handler;

    /// <summary>
    ///     Creates a handler for an interpolated string logged with the level <see cref="LogLevel.Warning" />.
    /// </summary>
    /// <param name="literalLength">The number of literal characters in the interpolated string.</param>
    /// <param name="formattedCount">The number of interpolated values.</param>
    /// <param name="logger">The logger the message is written to.</param>
    /// <param name="isEnabled">Whether the level is enabled. When <c>false</c>, the parts are not appended.</param>
    public StructuredLoggingWarningInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        _handler = new StructuredLoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Warning, out isEnabled);
    }

    /// <summary>
    ///     Whether the log level is enabled. Nothing is collected when it is disabled.
    /// </summary>
    public bool IsEnabled => _handler.IsEnabled;

    /// <inheritdoc cref="StructuredLoggingInterpolatedStringHandler.AppendLiteral" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string s) => _handler.AppendLiteral(s);

    /// <inheritdoc cref="StructuredLoggingInterpolatedStringHandler.AppendFormatted{T}" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, [CallerArgumentExpression("value")] string name = "") => _handler.AppendFormatted(value, name);

    /// <inheritdoc cref="StructuredLoggingInterpolatedStringHandler.GetTemplateAndArguments" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (string, object?[]) GetTemplateAndArguments() => _handler.GetTemplateAndArguments();
}
#endif
