using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace I18Next.Net.Internal;

internal sealed class DateFnsData
{
    private const string ResourceName = "I18Next.Net.Resources.date-fns.json.gz";
    private const string FallbackLocale = "en-US";

    private static readonly Lazy<DateFnsData> Instance = new(Load);

    private static readonly Dictionary<string, string> DefaultRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en-US",
        ["zh"] = "zh-CN"
    };

    private readonly Dictionary<string, DateFnsLocale> _locales;
    private readonly ConcurrentDictionary<string, DateFnsLocale> _resolvedLocales = new(StringComparer.OrdinalIgnoreCase);

    private DateFnsData(Dictionary<string, DateFnsLocale> locales)
    {
        _locales = locales;
    }

    public static DateFnsData Default => Instance.Value;

    public DateFnsLocale GetLocale(string language)
    {
        return _resolvedLocales.GetOrAdd(language ?? FallbackLocale, ResolveLocale);
    }

    private DateFnsLocale ResolveLocale(string language)
    {
        var locale = language.Replace('_', '-');

        if (_locales.TryGetValue(locale, out var result))
            return result;

        var languagePart = locale.Split('-')[0];

        if (_locales.TryGetValue(languagePart, out result))
            return result;

        if (DefaultRegions.TryGetValue(languagePart, out var defaultRegion) && _locales.TryGetValue(defaultRegion, out result))
            return result;

        var regional = _locales.Keys.Where(k => k.StartsWith(languagePart + "-", StringComparison.OrdinalIgnoreCase)).OrderBy(k => k).FirstOrDefault();

        return regional != null ? _locales[regional] : _locales[FallbackLocale];
    }

    private static DateFnsData Load()
    {
        using var stream = typeof(DateFnsData).Assembly.GetManifestResourceStream(ResourceName) ??
                           throw new InvalidOperationException($"The embedded resource {ResourceName} is missing.");
        using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzipStream);

        var root = document.RootElement;
        var ordinalTables = root.GetProperty("ordinals").EnumerateArray().Select(t => new OrdinalTable(
            [.. t.GetProperty("values").EnumerateArray().Select(v => v.GetString())],
            t.TryGetProperty("thousand", out var thousand) ? thousand.GetString() : null)).ToArray();

        var locales = new Dictionary<string, DateFnsLocale>(StringComparer.OrdinalIgnoreCase);

        foreach (var locale in root.GetProperty("locales").EnumerateObject())
        {
            var value = locale.Value;

            locales[locale.Name] = new DateFnsLocale(
                value.GetProperty("weekStartsOn").GetInt32(),
                value.GetProperty("firstWeekContainsDate").GetInt32(),
                value.GetProperty("formatLong").EnumerateObject()
                    .SelectMany(kind => kind.Value.EnumerateObject().Select(width => ($"{kind.Name}-{width.Name}", width.Value.GetString())))
                    .ToDictionary(e => e.Item1, e => e.Item2),
                ReadArrays(value.GetProperty("months")),
                ReadArrays(value.GetProperty("days")),
                ReadArrays(value.GetProperty("quarters")),
                ReadArrays(value.GetProperty("eras")),
                value.GetProperty("dayPeriods").EnumerateObject()
                    .SelectMany(width => width.Value.EnumerateObject().Select(period => ($"{width.Name}-{period.Name}", period.Value.GetString())))
                    .ToDictionary(e => e.Item1, e => e.Item2),
                value.GetProperty("digits").ValueKind == JsonValueKind.String ? value.GetProperty("digits").GetString() : null,
                value.GetProperty("removeDayOrdinalWithLongMonth").GetBoolean(),
                value.GetProperty("ordinals").EnumerateObject().ToDictionary(o => o.Name, o => ordinalTables[o.Value.GetInt32()]));
        }

        return new DateFnsData(locales);
    }

    private static Dictionary<string, string[]> ReadArrays(JsonElement element)
    {
        return element.EnumerateObject().ToDictionary(e => e.Name, e => e.Value.EnumerateArray().Select(v => v.GetString()).ToArray());
    }

    internal sealed class OrdinalTable(string[] values, string thousand)
    {
        public string Thousand { get; } = thousand;

        public string[] Values { get; } = values;
    }

    internal sealed class DateFnsLocale(int weekStartsOn, int firstWeekContainsDate, Dictionary<string, string> formatLong, Dictionary<string, string[]> months,
        Dictionary<string, string[]> days, Dictionary<string, string[]> quarters, Dictionary<string, string[]> eras,
        Dictionary<string, string> dayPeriods, string digits, bool removeDayOrdinalWithLongMonth, Dictionary<string, DateFnsData.OrdinalTable> ordinals)
    {
        public Dictionary<string, string> DayPeriods { get; } = dayPeriods;

        public Dictionary<string, string[]> Days { get; } = days;

        public string Digits { get; } = digits;

        public Dictionary<string, string[]> Eras { get; } = eras;

        public int FirstWeekContainsDate { get; } = firstWeekContainsDate;

        public Dictionary<string, string> FormatLong { get; } = formatLong;

        public Dictionary<string, string[]> Months { get; } = months;

        public Dictionary<string, OrdinalTable> Ordinals { get; } = ordinals;

        public bool RemoveDayOrdinalWithLongMonth { get; } = removeDayOrdinalWithLongMonth;

        public Dictionary<string, string[]> Quarters { get; } = quarters;

        public int WeekStartsOn { get; } = weekStartsOn;

        public string Ordinal(long number, string unit)
        {
            var table = Ordinals[unit];
            var absolute = Math.Abs(number);
            var entry = absolute < 200
                ? table.Values[absolute]
                : absolute % 1000 == 0 && table.Thousand != null
                    ? table.Thousand
                    : table.Values[100 + absolute % 100];

            var text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (Digits != null)
                text = new string([.. text.Select(c => c is >= '0' and <= '9' ? Digits[c - '0'] : c)]);

            return entry.Replace("{0}", text);
        }
    }
}
