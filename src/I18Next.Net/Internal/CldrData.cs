using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace I18Next.Net.Internal;

internal sealed class CldrData
{
    private const string ResourceName = "I18Next.Net.Resources.cldr.json.gz";
    private const string FallbackLocale = "en";

    private static readonly Lazy<CldrData> Instance = new(Load);

    private static readonly Dictionary<string, string> StyleFallbacks = new()
    {
        ["narrow"] = "short",
        ["short"] = "long"
    };

    private readonly Dictionary<string, Dictionary<string, object>> _calendars;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object>> _resolvedCalendars = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string[]>> _lists;
    private readonly Dictionary<string, string> _parents;
    private readonly Dictionary<string, Dictionary<string, RelativeTimeData>> _relativeTimes;

    private CldrData(Dictionary<string, string> parents, Dictionary<string, Dictionary<string, RelativeTimeData>> relativeTimes,
        Dictionary<string, Dictionary<string, string[]>> lists, Dictionary<string, Dictionary<string, object>> calendars)
    {
        _parents = parents;
        _relativeTimes = relativeTimes;
        _lists = lists;
        _calendars = calendars;
    }

    public static CldrData Default => Instance.Value;

    public IReadOnlyDictionary<string, object> GetCalendar(string language)
    {
        return _resolvedCalendars.GetOrAdd(language ?? FallbackLocale, ResolveCalendar);
    }

    public string[] GetListPatterns(string language, string type, string style)
    {
        return Find(_lists, language, type, style);
    }

    public RelativeTimeData GetRelativeTime(string language, string unit, string style)
    {
        return Find(_relativeTimes, language, unit, style);
    }

    private static Dictionary<string, string> ReadStrings(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
            result[property.Name] = property.Value.GetString();

        return result;
    }

    private static CldrData Load()
    {
        using var stream = typeof(CldrData).Assembly.GetManifestResourceStream(ResourceName) ??
                           throw new InvalidOperationException($"The embedded resource {ResourceName} is missing.");
        using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzipStream);

        var root = document.RootElement;
        var parents = ReadStrings(root.GetProperty("parents"));

        var relativeTimes = new Dictionary<string, Dictionary<string, RelativeTimeData>>(StringComparer.OrdinalIgnoreCase);

        foreach (var locale in root.GetProperty("relativeTime").EnumerateObject())
        {
            var entries = new Dictionary<string, RelativeTimeData>(StringComparer.Ordinal);

            foreach (var entry in locale.Value.EnumerateObject())
            {
                entries[entry.Name] = new RelativeTimeData(
                    ReadStrings(entry.Value.GetProperty("future")),
                    ReadStrings(entry.Value.GetProperty("past")),
                    entry.Value.TryGetProperty("relative", out var relative) ? ReadStrings(relative) : new Dictionary<string, string>());
            }

            relativeTimes[locale.Name] = entries;
        }

        var lists = new Dictionary<string, Dictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase);

        foreach (var locale in root.GetProperty("list").EnumerateObject())
        {
            var entries = new Dictionary<string, string[]>(StringComparer.Ordinal);

            foreach (var entry in locale.Value.EnumerateObject())
            {
                var patterns = new string[entry.Value.GetArrayLength()];
                var index = 0;

                foreach (var pattern in entry.Value.EnumerateArray())
                    patterns[index++] = pattern.GetString();

                entries[entry.Name] = patterns;
            }

            lists[locale.Name] = entries;
        }

        var calendars = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        foreach (var locale in root.GetProperty("calendar").EnumerateObject())
        {
            var entries = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var entry in locale.Value.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.Array)
                {
                    var values = new string[entry.Value.GetArrayLength()];
                    var index = 0;

                    foreach (var value in entry.Value.EnumerateArray())
                        values[index++] = value.GetString();

                    entries[entry.Name] = values;
                }
                else
                {
                    entries[entry.Name] = entry.Value.GetString();
                }
            }

            calendars[locale.Name] = entries;
        }

        return new CldrData(parents, relativeTimes, lists, calendars);
    }

    private IReadOnlyDictionary<string, object> ResolveCalendar(string language)
    {
        var chain = new List<Dictionary<string, object>>();

        foreach (var locale in GetLocaleChain(language))
        {
            if (_calendars.TryGetValue(locale, out var entries))
                chain.Add(entries);
        }

        var result = new Dictionary<string, object>(StringComparer.Ordinal);

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            foreach (var entry in chain[i])
                result[entry.Key] = entry.Value;
        }

        return result;
    }

    private TValue Find<TValue>(Dictionary<string, Dictionary<string, TValue>> table, string language, string name, string style)
        where TValue : class
    {
        foreach (var locale in GetLocaleChain(language))
        {
            if (!table.TryGetValue(locale, out var entries))
                continue;

            for (var currentStyle = style; currentStyle != null; StyleFallbacks.TryGetValue(currentStyle, out currentStyle))
            {
                if (entries.TryGetValue($"{name}-{currentStyle}", out var value))
                    return value;
            }
        }

        return null;
    }

    private IEnumerable<string> GetLocaleChain(string language)
    {
        var locale = NormalizeLocale(language);

        while (!string.IsNullOrEmpty(locale))
        {
            yield return locale;

            if (_parents.TryGetValue(locale, out var parent))
                locale = parent;
            else
                locale = locale.IndexOf('-') > -1 ? locale.Substring(0, locale.LastIndexOf('-')) : null;

            if (locale == "root")
                break;
        }

        yield return FallbackLocale;
    }

    private static string NormalizeLocale(string language)
    {
        if (string.IsNullOrEmpty(language))
            return FallbackLocale;

        var locale = language.Replace('_', '-');
        var parts = locale.Split('-');

        if (parts.Length == 2 && string.Equals(parts[0], "zh", StringComparison.OrdinalIgnoreCase) &&
            (parts[1].Equals("TW", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("HK", StringComparison.OrdinalIgnoreCase) ||
             parts[1].Equals("MO", StringComparison.OrdinalIgnoreCase)))
            return $"zh-Hant-{parts[1].ToUpperInvariant()}";

        return locale;
    }

    internal sealed class RelativeTimeData
    {
        public RelativeTimeData(Dictionary<string, string> future, Dictionary<string, string> past, Dictionary<string, string> relative)
        {
            Future = future;
            Past = past;
            Relative = relative;
        }

        public Dictionary<string, string> Future { get; }

        public Dictionary<string, string> Past { get; }

        public Dictionary<string, string> Relative { get; }
    }
}
