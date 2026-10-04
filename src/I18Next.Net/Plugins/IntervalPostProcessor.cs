using System.Collections.Generic;
using System.Text.RegularExpressions;

using I18Next.Net.Internal;

namespace I18Next.Net.Plugins;

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
