using System.Collections.Generic;
using System.Text.RegularExpressions;

using I18Next.Net.Internal;

namespace I18Next.Net.Plugins;

/// <summary>
///     Selects a value by the <c>count</c> argument from interval lists like
///     <c>(1){one item};(2-7){a few items};(7-inf){a lot of items}</c>, similar to the
///     i18next-intervalplural-postprocessor. Enabled per translation with the <c>postProcess: "interval"</c> argument.
/// </summary>
/// <remarks>
///     An interval is a single number, a range like <c>2-7</c> (both inclusive) or an open range like <c>inf-0</c> or
///     <c>7-inf</c>. The first matching interval wins. A missing or non-<see cref="int" /> count counts as 0.
/// </remarks>
public class IntervalPostProcessor : IPostProcessor
{
    /// <summary>
    ///     Matches a single interval like <c>(2-7){a few items}</c>, capturing the interval and the value.
    /// </summary>
    public static readonly Regex IntervalRegex = new(@"\((\S*)\).*{(.*)}");

    /// <summary>
    ///     The separator between the intervals. Defaults to <c>;</c>.
    /// </summary>
    public string IntervalSeparator { get; set; } = ";";

    /// <summary>
    ///     Whether the value of the first interval is used when no interval matches the count. Otherwise the unprocessed
    ///     translation is returned. Disabled by default.
    /// </summary>
    public bool UseFirstAsFallback { get; set; }

    /// <inheritdoc />
    public string Keyword => "interval";

    /// <inheritdoc />
    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    /// <inheritdoc />
    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        var intervals = value.Split(IntervalSeparator);

        if (!((args?.ContainsKey("count") ?? false) && args["count"] is int count))
            count = 0;

        string found = null;
        foreach (var entry in intervals)
        {
            var match = IntervalRegex.Match(entry);

            if (match.Success && CheckIntervalMatch(match.Groups[1].Value, count))
            {
                found = match.Groups[2].Value;
                break;
            }
        }

        // TODO Fallback to default plural translation
        return found ?? (UseFirstAsFallback ? GetFirstMatchValue(intervals[0]) : value);
    }

    private bool CheckIntervalMatch(string value, int count)
    {
        if (value.IndexOf('-') > -1)
        {
            var parts = value.Split('-');
            int from, to;

            // Negative infinity
            if (parts[0] == "inf")
            {
                return int.TryParse(parts[1], out to) && count <= to;
            }

            // Positive infinity
            if (parts[1] == "inf")
            {
                return int.TryParse(parts[0], out from) && count >= from;
            }

            // Both values set finite
            return int.TryParse(parts[0], out from) && int.TryParse(parts[1], out to) && count >= from && count <= to;
        }

        return int.TryParse(value, out var intervalNum) && intervalNum == count;
    }

    private string GetFirstMatchValue(string interval)
    {
        var match = IntervalRegex.Match(interval);

        return !match.Success ? interval : match.Groups[2].Value;
    }
}
