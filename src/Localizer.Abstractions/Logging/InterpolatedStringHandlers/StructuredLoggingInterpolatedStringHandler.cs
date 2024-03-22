#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace I18Next.Net.Logging.InterpolatedStringHandlers;

[InterpolatedStringHandler]
public ref struct StructuredLoggingInterpolatedStringHandler
{
    private readonly StringBuilder _template = null!;
    private readonly List<object?> _arguments = null!;

    public bool IsEnabled { get; }

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

    public void AppendLiteral(string s)
    {
        if (!IsEnabled)
            return;

        _template.Append(s.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal));
    }

    public void AppendFormatted<T>(
        T value,
        [CallerArgumentExpression("value")] string name = "")
    {
        if (!IsEnabled)
            return;

        _arguments.Add(value);
        _template.Append($"{{@{name}}}");
    }

    public (string, object?[]) GetTemplateAndArguments() => (_template.ToString(), _arguments.ToArray());
}

#endif
