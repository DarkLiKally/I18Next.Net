using System.Collections.Generic;
using System.Text.RegularExpressions;
using Localizer.Internal;

namespace Localizer.Plugins;

public class IntervalPostProcessor : IPostProcessor
{
    public static readonly Regex IntervalRegex = new(@"\((\S*)\).*{(.*)}");

    public string IntervalSeparator { get; set; } = ";";

    public bool UseFirstAsFallback { get; set; }

    public string Keyword => "interval";

    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

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
                if (!int.TryParse(parts[1], out to))
                    return false;

                return count <= to;
            }

            // Positive infinity
            if (parts[1] == "inf")
            {
                if (!int.TryParse(parts[0], out from))
                    return false;

                return count >= from;
            }

            // Both values set finite
            if (!int.TryParse(parts[0], out from) || !int.TryParse(parts[1], out to))
                return false;

            return count >= from && count <= to;
        }

        if (int.TryParse(value, out var intervalNum))

            return intervalNum == count;

        return false;
    }

    private string GetFirstMatchValue(string interval)
    {
        var match = IntervalRegex.Match(interval);

        if (!match.Success)
            return interval;

        return match.Groups[2].Value;
    }
}
