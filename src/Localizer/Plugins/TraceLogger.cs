using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Localizer.Logging;

namespace Localizer.Plugins;

/// <summary>
///     Logger implementation that forwards the log messages to the diagnostics trace logging system.
/// </summary>
public class TraceLogger : ILogger
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    private static readonly char[] FormatDelimiters = { ',', ':' };

    public bool IsEnabled(LogLevel logLevel)
    {
        return (int)LogLevel <= (int)logLevel;
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        var newMessage = message + Environment.NewLine + exception;
        Log(logLevel, newMessage, args);
    }

    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        var format = ConvertFormat(message);
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                Trace.TraceInformation(format, args);
                break;
            case LogLevel.Warning:
                Trace.TraceWarning(format, args);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Trace.TraceError(format, args);
                break;
        }
    }

    private string ConvertFormat(string format)
    {
        var valueNames = new List<string>();
        var sb = new StringBuilder();
        var scanIndex = 0;
        var endIndex = format.Length;

        while (scanIndex < endIndex)
        {
            var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
            var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

            // Format item syntax : { index[,alignment][ :formatString] }.
            var formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);

            if (closeBraceIndex == endIndex)
            {
                sb.Append(format, scanIndex, endIndex - scanIndex);
                scanIndex = endIndex;
            }
            else
            {
                sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
                sb.Append(valueNames.Count.ToString(CultureInfo.InvariantCulture));
                valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

                scanIndex = closeBraceIndex + 1;
            }
        }

        return sb.ToString();
    }

    private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
    {
        // Example: {{prefix{{{Argument}}}suffix}}.
        var braceIndex = endIndex;
        var scanIndex = startIndex;
        var braceOccurenceCount = 0;

        while (scanIndex < endIndex)
        {
            if (braceOccurenceCount > 0 && format[scanIndex] != brace)
            {
                if (braceOccurenceCount % 2 == 0)
                {
                    // Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
                    braceOccurenceCount = 0;
                    braceIndex = endIndex;
                }
                else
                {
                    // An unescaped '{' or '}' found.
                    break;
                }
            }
            else if (format[scanIndex] == brace)
            {
                if (brace == '}')
                {
                    if (braceOccurenceCount == 0)
                    {
                        // For '}' pick the first occurence.
                        braceIndex = scanIndex;
                    }
                }
                else
                {
                    // For '{' pick the last occurence.
                    braceIndex = scanIndex;
                }

                braceOccurenceCount++;
            }

            scanIndex++;
        }

        return braceIndex;
    }

    private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
    {
        var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
        return findIndex == -1 ? endIndex : findIndex;
    }
}
