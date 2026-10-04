using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace I18Next.Net.Internal;

internal static class LdmlDateFormat
{
    private const string DateFieldTypes = "GyMdE";

    private static readonly ConcurrentDictionary<(string Language, string Skeleton, bool ExplicitHourCycle), string> SkeletonPatterns = new();

    public static string Format(DateTimeOffset value, string pattern, string language)
    {
        var calendar = CldrData.Default.GetCalendar(language);
        var digits = GetString(calendar, "digits") ?? "0123456789";
        var result = new StringBuilder(pattern.Length * 2);
        var fields = Parse(pattern);
        var hasMinutes = fields.Any(f => f.Letter == 'm');

        foreach (var field in fields)
        {
            if (field.Letter == '\0')
                result.Append(field.Literal);
            else
                result.Append(FormatField(value, field.Letter, field.Width, calendar, digits, hasMinutes));
        }

        return result.Replace('\u202F', ' ').ToString();
    }

    public static string GetStylePattern(string language, string dateStyle, string timeStyle, char? hourLetter = null)
    {
        var calendar = CldrData.Default.GetCalendar(language);
        var timePattern = timeStyle == null ? null : GetString(calendar, $"time-{timeStyle}");

        if (timePattern != null && hourLetter.HasValue)
            timePattern = ApplyHourCycle(language, timePattern, hourLetter.Value);

        if (dateStyle != null && timeStyle != null)
        {
            var combination = GetString(calendar, $"dateTimeAtTime-{dateStyle}") ?? GetString(calendar, $"dateTime-{dateStyle}");

            return CombineDateTime(combination, GetString(calendar, $"date-{dateStyle}"), timePattern);
        }

        return dateStyle != null ? GetString(calendar, $"date-{dateStyle}") : timePattern;
    }

    private static string ApplyHourCycle(string language, string timePattern, char hourLetter)
    {
        var fields = Parse(timePattern);
        var hour = fields.FirstOrDefault(f => GetFieldType(f.Letter) == 'h');

        if (hour.Letter == '\0' || Is12HourLetter(hour.Letter) == Is12HourLetter(hourLetter))
            return timePattern;

        var skeleton = new StringBuilder();

        foreach (var field in fields)
        {
            if (field.Letter == '\0' || field.Letter is 'a' or 'b' or 'B')
                continue;

            skeleton.Append(GetFieldType(field.Letter) == 'h' ? hourLetter : field.Letter, field.Width);
        }

        return GetSkeletonPattern(language, skeleton.ToString(), true);
    }

    public static string GetSkeletonPattern(string language, string skeleton, bool explicitHourCycle = false)
    {
        return SkeletonPatterns.GetOrAdd((language, skeleton, explicitHourCycle),
            key => ReplaceFlexibleDayPeriods(BuildSkeletonPattern(key.Language, key.Skeleton, key.ExplicitHourCycle)));
    }

    private static string ReplaceFlexibleDayPeriods(string pattern)
    {
        var fields = Parse(pattern);

        if (fields.All(f => f.Letter != 'B'))
            return pattern;

        var result = new StringBuilder(pattern.Length);

        foreach (var field in fields)
        {
            if (field.Letter == '\0')
                result.Append(QuoteLiteral(field.Literal));
            else
                result.Append(field.Letter == 'B' ? 'a' : field.Letter, field.Letter == 'B' ? 1 : field.Width);
        }

        return result.ToString();
    }

    public static string GetDecimalSeparator(string language)
    {
        return GetString(CldrData.Default.GetCalendar(language), "decimal") ?? ".";
    }

    public static char GetPreferredHourLetter(string language)
    {
        var shortTime = GetString(CldrData.Default.GetCalendar(language), "time-short") ?? "HH:mm";

        foreach (var field in Parse(shortTime))
        {
            if (field.Letter is 'h' or 'H' or 'K' or 'k')
                return field.Letter;
        }

        return 'H';
    }

    private static string BuildSkeletonPattern(string language, string skeleton, bool explicitHourCycle)
    {
        var calendar = CldrData.Default.GetCalendar(language);
        var requested = ParseSkeleton(skeleton);

        var preferredHour = GetPreferredHourLetter(language);
        var pattern = FindBestPattern(calendar, requested, out var exactMatch, out var matchedSkeleton);

        if (pattern != null)
            return exactMatch && !explicitHourCycle ? pattern : AdjustPattern(pattern, requested, matchedSkeleton, preferredHour, explicitHourCycle);

        var dateFields = requested.Where(f => DateFieldTypes.IndexOf(GetFieldType(f.Letter)) > -1).ToList();
        var timeFields = requested.Where(f => DateFieldTypes.IndexOf(GetFieldType(f.Letter)) < 0).ToList();

        if (dateFields.Count == 0 || timeFields.Count == 0)
            return GetString(calendar, dateFields.Count > 0 ? "date-short" : "time-short");

        var datePattern = FindBestPattern(calendar, dateFields, out var exactDate, out var dateSkeleton);
        var timePattern = FindBestPattern(calendar, timeFields, out var exactTime, out var timeSkeleton);

        datePattern = datePattern == null
            ? GetString(calendar, "date-short")
            : exactDate ? datePattern : AdjustPattern(datePattern, dateFields, dateSkeleton, preferredHour, explicitHourCycle);
        timePattern = timePattern == null
            ? GetString(calendar, "time-short")
            : exactTime && !explicitHourCycle ? timePattern : AdjustPattern(timePattern, timeFields, timeSkeleton, preferredHour, explicitHourCycle);

        var month = dateFields.FirstOrDefault(f => f.Letter == 'M');
        var style = month.Width >= 4 ? dateFields.Any(f => f.Letter == 'E') ? "full" : "long" : month.Width == 3 ? "medium" : "short";
        var combination = GetString(calendar, $"dateTimeAtTime-{style}") ?? GetString(calendar, $"dateTime-{style}");

        return CombineDateTime(combination, datePattern, timePattern);
    }

    private static string CombineDateTime(string combination, string datePattern, string timePattern)
    {
        return combination.Replace("{1}", datePattern).Replace("{0}", timePattern);
    }

    private static string FindBestPattern(IReadOnlyDictionary<string, object> calendar, List<Field> requested, out bool exactMatch,
        out List<Field> matchedSkeleton)
    {
        string bestPattern = null;
        exactMatch = false;
        matchedSkeleton = null;
        var bestDistance = int.MaxValue;
        var requestedTypes = new string([.. requested.Select(f => GetFieldType(f.Letter)).OrderBy(c => c)]);

        foreach (var (candidate, candidatePattern, isAvailableFormat) in GetCandidates(calendar))
        {
            var candidateTypes = new string([.. candidate.Select(f => GetFieldType(f.Letter)).OrderBy(c => c)]);

            if (candidateTypes != requestedTypes || candidate.Any(f => f.Letter == 'B'))
                continue;

            var distance = 0;

            foreach (var field in requested)
            {
                var match = candidate.First(f => GetFieldType(f.Letter) == GetFieldType(field.Letter));

                if (match.Letter != field.Letter && !(GetFieldType(field.Letter) == 'E' && IsText(match) && IsText(field)))
                    distance += GetFieldType(field.Letter) == 'h' ? 1000 : 10;

                if (IsText(match) != IsText(field))
                    distance += 100;
                else
                    distance += Math.Abs(NormalizedWidth(match) - NormalizedWidth(field));
            }

            if (distance < bestDistance || distance == bestDistance && isAvailableFormat)
            {
                bestDistance = distance;
                bestPattern = candidatePattern;
                exactMatch = distance == 0;
                matchedSkeleton = candidate;
            }
        }

        return bestPattern;
    }

    private static bool Is12HourLetter(char letter)
    {
        return letter is 'h' or 'K';
    }

    private static string GetBaseSkeleton(IEnumerable<Field> fields)
    {
        return string.Concat(fields.Select(f => GetFieldType(f.Letter) + (IsText(f) ? NormalizedWidth(f).ToString(CultureInfo.InvariantCulture) : "n"))
            .OrderBy(x => x, StringComparer.Ordinal));
    }

    private static IEnumerable<(List<Field> Skeleton, string Pattern, bool IsAvailableFormat)> GetCandidates(IReadOnlyDictionary<string, object> calendar)
    {
        var availableBases = new HashSet<string>(calendar.Keys
            .Where(k => k.StartsWith("skeleton-", StringComparison.Ordinal))
            .Select(k => GetBaseSkeleton(ParseSkeleton(k.Substring(9)))));

        foreach (var style in new[] { "full", "long", "medium", "short" })
        {
            foreach (var kind in new[] { "date", "time" })
            {
                if (calendar.TryGetValue($"{kind}-{style}", out var value) && value is string pattern)
                {
                    var skeleton = Parse(pattern).Where(f => f.Letter != '\0' && f.Letter is not ('a' or 'b' or 'B')).ToList();

                    if (skeleton.Count > 0 && !availableBases.Contains(GetBaseSkeleton(skeleton)))
                        yield return (skeleton, pattern, false);
                }
            }
        }

        foreach (var entry in calendar)
        {
            if (entry.Key.StartsWith("skeleton-", StringComparison.Ordinal))
                yield return (ParseSkeleton(entry.Key.Substring(9)), (string)entry.Value, true);
        }
    }

    private static int NormalizedWidth(Field field)
    {
        switch (GetFieldType(field.Letter))
        {
            case 'E' when IsText(field):
            case 'G':
                return Math.Max(field.Width, 3);
            default:
                return field.Width;
        }
    }

    private static string AdjustPattern(string pattern, List<Field> requested, List<Field> skeleton, char preferredHour, bool explicitHourCycle)
    {
        var result = new StringBuilder(pattern.Length);

        foreach (var field in Parse(pattern))
        {
            if (field.Letter == '\0')
            {
                result.Append(QuoteLiteral(field.Literal));
                continue;
            }

            var type = GetFieldType(field.Letter);
            var match = requested.FirstOrDefault(f => GetFieldType(f.Letter) == type);
            var letter = field.Letter;
            var width = field.Width;

            var skeletonField = skeleton.FirstOrDefault(f => GetFieldType(f.Letter) == type);
            var sameAsSkeleton = skeletonField.Letter != '\0' && IsText(skeletonField) == IsText(match) &&
                                 NormalizedWidth(skeletonField) == NormalizedWidth(match);
            var sameKind = type is not ('M' or 'E') || IsText(field) == IsText(match);

            if (match.Letter != '\0' && (type == 'h' || !sameAsSkeleton && sameKind))
            {
                switch (type)
                {
                    case 'M':
                    case 'E':
                    case 'G':
                    case 'd':
                        width = match.Width;
                        break;
                    case 'y':
                        width = match.Width == 2 ? 2 : field.Width;
                        break;
                    case 'h':
                        if (explicitHourCycle || Is12HourLetter(field.Letter) != Is12HourLetter(match.Letter))
                            letter = match.Letter;

                        width = Is12HourLetter(match.Letter) == Is12HourLetter(preferredHour) && requested.All(f => GetFieldType(f.Letter) != 'z')
                            ? match.Width
                            : Math.Max(field.Width, match.Width);
                        break;
                    case 'm':
                    case 's':
                        width = Math.Max(field.Width, match.Width);
                        break;
                    case 'z':
                        letter = match.Letter;
                        width = match.Width;
                        break;
                }

                if (type == 'E' && field.Letter is 'c' or 'e' && match.Width < 3)
                    letter = 'E';
            }

            result.Append(letter, width);
        }

        return result.ToString();
    }

    private static List<Field> ParseSkeleton(string skeleton)
    {
        var fields = new List<Field>();

        for (var i = 0; i < skeleton.Length;)
        {
            var letter = skeleton[i];
            var width = 1;

            while (i + width < skeleton.Length && skeleton[i + width] == letter)
                width++;

            fields.Add(new Field(letter, width, null));
            i += width;
        }

        return fields;
    }

    private static char GetFieldType(char letter)
    {
        return letter switch
        {
            'L' => 'M',
            'c' or 'e' => 'E',
            'H' or 'K' or 'k' or 'j' => 'h',
            'v' or 'V' or 'O' => 'z',
            'Y' or 'u' => 'y',
            _ => letter,
        };
    }

    private static bool IsText(Field field)
    {
        return GetFieldType(field.Letter) switch
        {
            'M' => field.Width >= 3,
            'E' => field.Letter == 'E' || field.Width >= 3,
            'G' => true,
            _ => false,
        };
    }

    public static List<Field> Parse(string pattern)
    {
        var fields = new List<Field>();
        var literal = new StringBuilder();

        for (var i = 0; i < pattern.Length;)
        {
            var c = pattern[i];

            if (c == '\'')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
                {
                    literal.Append('\'');
                    i += 2;
                    continue;
                }

                var end = i + 1;

                while (end < pattern.Length)
                {
                    if (pattern[end] == '\'')
                    {
                        if (end + 1 < pattern.Length && pattern[end + 1] == '\'')
                        {
                            literal.Append('\'');
                            end += 2;
                            continue;
                        }

                        break;
                    }

                    literal.Append(pattern[end]);
                    end++;
                }

                i = end + 1;
                continue;
            }

            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            {
                if (literal.Length > 0)
                {
                    fields.Add(new Field('\0', 0, literal.ToString()));
                    literal.Clear();
                }

                var width = 1;

                while (i + width < pattern.Length && pattern[i + width] == c)
                    width++;

                fields.Add(new Field(c, width, null));
                i += width;
                continue;
            }

            literal.Append(c);
            i++;
        }

        if (literal.Length > 0)
            fields.Add(new Field('\0', 0, literal.ToString()));

        return fields;
    }

    private static string QuoteLiteral(string literal)
    {
        var needsQuotes = false;

        foreach (var c in literal)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '\'')
            {
                needsQuotes = true;
                break;
            }
        }

        return needsQuotes ? "'" + literal.Replace("'", "''") + "'" : literal;
    }

    private static string FormatField(DateTimeOffset value, char letter, int width, IReadOnlyDictionary<string, object> calendar, string digits,
        bool hasMinutes)
    {
        switch (letter)
        {
            case 'G':
                return GetArray(calendar, $"eras-{GetWidthName(width, 4, 5)}")[1];
            case 'y':
            case 'Y':
            case 'u':
                return width == 2 ? Number(value.Year % 100, 2, digits) : Number(value.Year, width, digits);
            case 'Q':
            case 'q':
                var quarter = (value.Month - 1) / 3;
                return width <= 2
                    ? Number(quarter + 1, width, digits)
                    : GetArray(calendar, $"quarters-{(letter == 'Q' ? "format" : "stand-alone")}-{GetWidthName(width, 4, 5)}")[quarter];
            case 'M':
            case 'L':
                return width <= 2
                    ? Number(value.Month, width, digits)
                    : GetArray(calendar, $"months-{(letter == 'M' ? "format" : "stand-alone")}-{GetWidthName(width, 4, 5)}")[value.Month - 1];
            case 'd':
                return Number(value.Day, width, digits);
            case 'D':
                return Number(value.DayOfYear, width, digits);
            case 'w':
                return Number(GetIsoWeek(value.DateTime), width, digits);
            case 'E':
            case 'e':
            case 'c':
                if (letter != 'E' && width <= 2)
                    return Number((int)value.DayOfWeek + 1, width, digits);

                return GetArray(calendar, $"days-{(letter == 'c' ? "stand-alone" : "format")}-{GetDayWidthName(letter == 'E' ? Math.Max(width, 3) : width)}")[
                    (int)value.DayOfWeek];
            case 'a':
            case 'b':
                return GetArray(calendar, $"dayPeriods-format-{(width >= 5 ? "narrow" : "wide")}")[value.Hour < 12 ? 0 : 1];
            case 'B':
                return GetFlexibleDayPeriod(value, GetWidthName(width, 4, 5), calendar, hasMinutes) ??
                       GetArray(calendar, $"dayPeriods-format-{(width >= 5 ? "narrow" : "wide")}")[value.Hour < 12 ? 0 : 1];
            case 'h':
                return Number(value.Hour % 12 == 0 ? 12 : value.Hour % 12, width, digits);
            case 'H':
                return Number(value.Hour, width, digits);
            case 'K':
                return Number(value.Hour % 12, width, digits);
            case 'k':
                return Number(value.Hour == 0 ? 24 : value.Hour, width, digits);
            case 'm':
                return Number(value.Minute, width, digits);
            case 's':
                return Number(value.Second, width, digits);
            case 'S':
                var fraction = (value.Ticks % TimeSpan.TicksPerSecond).ToString("0000000", CultureInfo.InvariantCulture);
                return ToDigits(width <= 7 ? fraction.Substring(0, width) : fraction.PadRight(width, '0'), digits);
            case 'z':
            case 'O':
            case 'v':
            case 'V':
                return FormatGmt(value.Offset, width >= 4, calendar, digits);
            case 'Z':
                if (width == 4)
                    return FormatGmt(value.Offset, true, calendar, digits);

                return FormatIsoOffset(value.Offset, width == 5, width == 5, digits);
            case 'X':
            case 'x':
                return FormatIsoOffset(value.Offset, width >= 3 && width != 4, letter == 'X', digits, width == 1);
            default:
                return new string(letter, width);
        }
    }

    private static string GetFlexibleDayPeriod(DateTimeOffset value, string width, IReadOnlyDictionary<string, object> calendar, bool hasMinutes)
    {
        if (!calendar.TryGetValue("dayPeriodRules", out var rulesValue) || !calendar.TryGetValue($"flexibleDayPeriods-format-{width}", out var namesValue))
            return null;

        var minutes = value.Hour * 60 + value.Minute;
        string period = null;

        foreach (var rule in (string[])rulesValue)
        {
            var parts = rule.Split('|');

            if (parts[1].Length > 0)
            {
                if (!hasMinutes && value.Minute == 0 && ParseTime(parts[1]) == minutes)
                {
                    period = parts[0];
                    break;
                }

                continue;
            }

            var from = ParseTime(parts[2]);
            var before = ParseTime(parts[3]);

            if (from < before ? minutes >= from && minutes < before : minutes >= from || minutes < before)
                period = parts[0];
        }

        if (period == null)
            return null;

        foreach (var name in (string[])namesValue)
        {
            if (name.StartsWith(period + "=", StringComparison.Ordinal))
                return name.Substring(period.Length + 1);
        }

        return null;
    }

    private static int ParseTime(string time)
    {
        var parts = time.Split(':');

        return int.Parse(parts[0], CultureInfo.InvariantCulture) * 60 + int.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    private static string FormatGmt(TimeSpan offset, bool longFormat, IReadOnlyDictionary<string, object> calendar, string digits)
    {
        if (offset == TimeSpan.Zero)
            return GetString(calendar, "gmtZeroFormat") ?? "GMT";

        var hourFormats = (GetString(calendar, "hourFormat") ?? "+HH:mm;-HH:mm").Split(';');
        var hourFormat = offset < TimeSpan.Zero ? hourFormats[1] : hourFormats[0];
        var absolute = offset.Duration();
        string formatted;

        if (longFormat)
        {
            formatted = hourFormat.Replace("HH", Number(absolute.Hours, 2, digits)).Replace("mm", Number(absolute.Minutes, 2, digits));
        }
        else
        {
            var hoursOnly = hourFormat.Substring(0, hourFormat.IndexOf('H')) + "H";
            formatted = absolute.Minutes == 0
                ? hoursOnly.Replace("H", Number(absolute.Hours, 1, digits))
                : hourFormat.Replace("HH", Number(absolute.Hours, 1, digits)).Replace("mm", Number(absolute.Minutes, 2, digits));
        }

        return (GetString(calendar, "gmtFormat") ?? "GMT{0}").Replace("{0}", formatted);
    }

    private static string FormatIsoOffset(TimeSpan offset, bool withColon, bool useZ, string digits, bool optionalMinutes = false)
    {
        if (useZ && offset == TimeSpan.Zero)
            return "Z";

        var absolute = offset.Duration();
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var hours = Number(absolute.Hours, 2, digits);

        return optionalMinutes && absolute.Minutes == 0
            ? sign + hours
            : sign + hours + (withColon ? ":" : "") + Number(absolute.Minutes, 2, digits);
    }

    private static int GetIsoWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        var thursday = date.AddDays(3 - (day + 6) % 7);

        return (thursday.DayOfYear - 1) / 7 + 1;
    }

    private static string GetWidthName(int width, int wide, int narrow)
    {
        return width >= narrow ? "narrow" : width == wide ? "wide" : "abbreviated";
    }

    private static string GetDayWidthName(int width)
    {
        return width switch
        {
            4 => "wide",
            5 => "narrow",
            6 => "short",
            _ => "abbreviated"
        };
    }

    private static string Number(int value, int width, string digits)
    {
        return ToDigits(value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0'), digits);
    }

    private static string ToDigits(string value, string digits)
    {
        if (digits == "0123456789")
            return value;

        var chars = value.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
                chars[i] = digits[chars[i] - '0'];
        }

        return new string(chars);
    }

    private static string GetString(IReadOnlyDictionary<string, object> calendar, string key)
    {
        return calendar.TryGetValue(key, out var value) ? value as string : null;
    }

    private static string[] GetArray(IReadOnlyDictionary<string, object> calendar, string key)
    {
        return calendar.TryGetValue(key, out var value) ? (string[])value : throw new KeyNotFoundException(key);
    }

    internal readonly struct Field(char letter, int width, string literal)
    {
        public char Letter { get; } = letter;

        public int Width { get; } = width;

        public string Literal { get; } = literal;
    }
}
