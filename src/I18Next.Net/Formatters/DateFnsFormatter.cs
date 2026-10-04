using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

/// <summary>
///     Formats DateTime and DateTimeOffset values with date-fns format strings, e.g. <c>{{date, do MMMM yyyy}}</c> or
///     <c>{{date, PPPp}}</c>, using the locale data of date-fns.
/// </summary>
public class DateFnsFormatter : IFormatter
{
    private static readonly Regex LongFormattingTokensRegex = new(@"P+p+|P+|p+|''|'(''|[^'])+('|$)|.", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex FormattingTokensRegex =
        new(@"[yYQqMLwIdDecihHKkms]o|([A-Za-z0-9_])\1*|''|'(''|[^'])+('|$)|.", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex DateTimeLongRegex = new("(P+)(p+)?", RegexOptions.Compiled);

    /// <summary>
    ///     Overrides the first day of the week of the locale (0 = Sunday).
    /// </summary>
    public int? WeekStartsOn { get; set; }

    /// <summary>
    ///     Overrides the day of January which is always in the first week of the year.
    /// </summary>
    public int? FirstWeekContainsDate { get; set; }

    public bool CanFormat(object value, string format, string language)
    {
        return (value is DateTime || value is DateTimeOffset) && format != null && !IntlFormatter.IsFormatName(format);
    }

    public string Format(object value, string format, string language)
    {
        var date = value is DateTimeOffset dateTimeOffset ? dateTimeOffset : ToDateTimeOffset((DateTime) value);
        var locale = DateFnsData.Default.GetLocale(language);
        var weekStartsOn = WeekStartsOn ?? locale.WeekStartsOn;
        var firstWeekContainsDate = FirstWeekContainsDate ?? locale.FirstWeekContainsDate;

        var expanded = LongFormattingTokensRegex.Replace(format, match => ExpandLongFormat(match.Value, locale));
        var result = new StringBuilder(expanded.Length * 2);
        var tokens = FormattingTokensRegex.Matches(expanded).Cast<Match>().Select(m => m.Value).ToList();

        if (locale.RemoveDayOrdinalWithLongMonth && date.Day != 1 && tokens.Any(t => t is "MMM" or "MMMM"))
            tokens = tokens.Select(t => t == "do" ? "d" : t).ToList();

        foreach (var token in tokens)
        {

            if (token == "''")
            {
                result.Append('\'');
                continue;
            }

            if (token[0] == '\'')
            {
                result.Append(CleanEscapedString(token));
                continue;
            }

            result.Append(FormatToken(date, token, locale, weekStartsOn, firstWeekContainsDate) ?? token);
        }

        return result.ToString();
    }

    private static string ExpandLongFormat(string token, DateFnsData.DateFnsLocale locale)
    {
        switch (token[0])
        {
            case 'p':
                return locale.FormatLong[$"time-{GetLongWidth(token.Length)}"];
            case 'P':
                var match = DateTimeLongRegex.Match(token);
                var datePattern = match.Groups[1].Value;
                var timePattern = match.Groups[2].Value;
                var date = locale.FormatLong[$"date-{GetLongWidth(datePattern.Length)}"];

                if (timePattern.Length == 0)
                    return date;

                return locale.FormatLong[$"dateTime-{GetLongWidth(datePattern.Length)}"]
                    .Replace("{{date}}", date)
                    .Replace("{{time}}", locale.FormatLong[$"time-{GetLongWidth(timePattern.Length)}"]);
            default:
                return token;
        }
    }

    private static string GetLongWidth(int length)
    {
        return length switch
        {
            1 => "short",
            2 => "medium",
            3 => "long",
            _ => "full"
        };
    }

    private static string CleanEscapedString(string token)
    {
        var content = token.Length > 1 && token[token.Length - 1] == '\'' ? token.Substring(1, token.Length - 2) : token.Substring(1);

        return content.Replace("''", "'");
    }

    private static string FormatToken(DateTimeOffset date, string token, DateFnsData.DateFnsLocale locale, int weekStartsOn, int firstWeekContainsDate)
    {
        var length = token.Length;
        var ordinal = length == 2 && token[1] == 'o';
        var dayOfWeek = (int) date.DayOfWeek;
        var hours = date.Hour;

        switch (token[0])
        {
            case 'G':
                return locale.Eras[GetWidth(length, 3, 4, 5)][date.Year > 0 ? 1 : 0];
            case 'y':
                if (ordinal)
                    return locale.Ordinal(date.Year, "year");

                return Pad(token == "yy" ? date.Year % 100 : date.Year, length);
            case 'Y':
                var weekYear = GetWeekYear(date.DateTime, weekStartsOn, firstWeekContainsDate);

                if (ordinal)
                    return locale.Ordinal(weekYear, "year");

                return Pad(token == "YY" ? weekYear % 100 : weekYear, length);
            case 'R':
                return Pad(GetWeekYear(date.DateTime, 1, 4), length);
            case 'u':
                return Pad(date.Year, length);
            case 'Q':
            case 'q':
                var quarter = (date.Month + 2) / 3;

                if (ordinal)
                    return locale.Ordinal(quarter, "quarter");

                return length <= 2
                    ? Pad(quarter, length)
                    : locale.Quarters[$"{(token[0] == 'Q' ? "formatting" : "standalone")}-{GetWidth(length, 3, 4, 5)}"][quarter - 1];
            case 'M':
            case 'L':
                if (ordinal)
                    return locale.Ordinal(date.Month, "month");

                if (length <= 2)
                    return token[0] == 'M' && length == 1 ? date.Month.ToString(CultureInfo.InvariantCulture) : Pad(date.Month, length == 1 ? 1 : 2);

                return locale.Months[$"{(token[0] == 'M' ? "formatting" : "standalone")}-{GetWidth(length, 3, 4, 5)}"][date.Month - 1];
            case 'w':
                var week = GetWeek(date.DateTime, weekStartsOn, firstWeekContainsDate);

                return ordinal ? locale.Ordinal(week, "week") : Pad(week, length);
            case 'I':
                var isoWeek = GetWeek(date.DateTime, 1, 4);

                return ordinal ? locale.Ordinal(isoWeek, "week") : Pad(isoWeek, length);
            case 'd':
                return ordinal ? locale.Ordinal(date.Day, "date") : Pad(date.Day, length);
            case 'D':
                return ordinal ? locale.Ordinal(date.DayOfYear, "dayOfYear") : Pad(date.DayOfYear, length);
            case 'E':
                return locale.Days[$"formatting-{GetDayWidth(length <= 3 ? 3 : length)}"][dayOfWeek];
            case 'e':
            case 'c':
                var localDayOfWeek = (dayOfWeek - weekStartsOn + 8) % 7;
                localDayOfWeek = localDayOfWeek == 0 ? 7 : localDayOfWeek;

                if (ordinal)
                    return locale.Ordinal(localDayOfWeek, "day");

                if (length <= 2)
                    return token[0] == 'e' && length == 2 ? Pad(localDayOfWeek, 2) : Pad(localDayOfWeek, length);

                return locale.Days[$"{(token[0] == 'e' ? "formatting" : "standalone")}-{GetDayWidth(length)}"][dayOfWeek];
            case 'i':
                var isoDayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;

                if (ordinal)
                    return locale.Ordinal(isoDayOfWeek, "day");

                return length <= 2 ? Pad(isoDayOfWeek, length) : locale.Days[$"formatting-{GetDayWidth(length)}"][dayOfWeek];
            case 'a':
                var amPm = hours >= 12 ? "pm" : "am";

                return FormatDayPeriod(locale, amPm, length);
            case 'b':
                var period = hours == 12 ? "noon" : hours == 0 ? "midnight" : hours >= 12 ? "pm" : "am";

                return FormatDayPeriod(locale, period, length);
            case 'B':
                var flexiblePeriod = hours >= 17 ? "evening" : hours >= 12 ? "afternoon" : hours >= 4 ? "morning" : "night";

                return locale.DayPeriods[$"{GetWidth(length, 3, 4, 5)}-{flexiblePeriod}"];
            case 'h':
                var hours12 = hours % 12 == 0 ? 12 : hours % 12;

                return ordinal ? locale.Ordinal(hours12, "hour") : Pad(hours12, length);
            case 'H':
                return ordinal ? locale.Ordinal(hours, "hour") : Pad(hours, length);
            case 'K':
                return ordinal ? locale.Ordinal(hours % 12, "hour") : Pad(hours % 12, length);
            case 'k':
                var hours24 = hours == 0 ? 24 : hours;

                return ordinal ? locale.Ordinal(hours24, "hour") : Pad(hours24, length);
            case 'm':
                return ordinal ? locale.Ordinal(date.Minute, "minute") : Pad(date.Minute, length);
            case 's':
                return ordinal ? locale.Ordinal(date.Second, "second") : Pad(date.Second, length);
            case 'S':
                var fraction = (long) Math.Truncate(date.Millisecond * Math.Pow(10, length - 3));

                return Pad(fraction, length);
            case 'X':
                if (date.Offset == TimeSpan.Zero)
                    return "Z";

                return FormatIsoTimezone(date.Offset, length);
            case 'x':
                return FormatIsoTimezone(date.Offset, length);
            case 'O':
            case 'z':
                return "GMT" + (length <= 3 ? FormatTimezoneShort(date.Offset) : FormatTimezone(date.Offset, ":"));
            case 't':
                return (date.ToUnixTimeMilliseconds() / 1000).ToString(CultureInfo.InvariantCulture);
            case 'T':
                return date.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            default:
                return null;
        }
    }

    private static string FormatDayPeriod(DateFnsData.DateFnsLocale locale, string period, int length)
    {
        switch (length)
        {
            case 1:
            case 2:
                return locale.DayPeriods[$"abbreviated-{period}"];
            case 3:
                return locale.DayPeriods[$"abbreviated-{period}"].ToLowerInvariant();
            case 5:
                return locale.DayPeriods[$"narrow-{period}"];
            default:
                return locale.DayPeriods[$"wide-{period}"];
        }
    }

    private static string FormatIsoTimezone(TimeSpan offset, int length)
    {
        switch (length)
        {
            case 1:
                return offset.Minutes == 0 ? Sign(offset) + Pad(Math.Abs(offset.Hours), 2) : FormatTimezone(offset, "");
            case 2:
            case 4:
                return FormatTimezone(offset, "");
            default:
                return FormatTimezone(offset, ":");
        }
    }

    private static string FormatTimezone(TimeSpan offset, string delimiter)
    {
        var absolute = offset.Duration();

        return Sign(offset) + Pad(absolute.Hours, 2) + delimiter + Pad(absolute.Minutes, 2);
    }

    private static string FormatTimezoneShort(TimeSpan offset)
    {
        var absolute = offset.Duration();
        var hours = absolute.Hours.ToString(CultureInfo.InvariantCulture);

        return absolute.Minutes == 0 ? Sign(offset) + hours : Sign(offset) + hours + ":" + Pad(absolute.Minutes, 2);
    }

    private static string Sign(TimeSpan offset)
    {
        return offset < TimeSpan.Zero ? "-" : "+";
    }

    private static string GetWidth(int length, int abbreviated, int wide, int narrow)
    {
        if (length == narrow)
            return "narrow";

        return length == wide || length > narrow ? "wide" : "abbreviated";
    }

    private static string GetDayWidth(int length)
    {
        return length switch
        {
            3 => "abbreviated",
            5 => "narrow",
            6 => "short",
            _ => "wide"
        };
    }

    private static string Pad(long number, int length)
    {
        var sign = number < 0 ? "-" : "";

        return sign + Math.Abs(number).ToString(CultureInfo.InvariantCulture).PadLeft(length, '0');
    }

    private static DateTime StartOfWeek(DateTime date, int weekStartsOn)
    {
        var day = (int) date.DayOfWeek;
        var difference = (day < weekStartsOn ? 7 : 0) + day - weekStartsOn;

        return date.Date.AddDays(-difference);
    }

    private static int GetWeekYear(DateTime date, int weekStartsOn, int firstWeekContainsDate)
    {
        var year = date.Year;
        var firstWeekOfNextYear = StartOfWeek(new DateTime(year + 1, 1, firstWeekContainsDate), weekStartsOn);
        var firstWeekOfThisYear = StartOfWeek(new DateTime(year, 1, firstWeekContainsDate), weekStartsOn);

        if (date >= firstWeekOfNextYear)
            return year + 1;

        return date >= firstWeekOfThisYear ? year : year - 1;
    }

    private static int GetWeek(DateTime date, int weekStartsOn, int firstWeekContainsDate)
    {
        var weekYear = GetWeekYear(date, weekStartsOn, firstWeekContainsDate);
        var startOfWeekYear = StartOfWeek(new DateTime(weekYear, 1, firstWeekContainsDate), weekStartsOn);

        return (int) Math.Round((StartOfWeek(date, weekStartsOn) - startOfWeekYear).TotalDays / 7) + 1;
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return new DateTimeOffset(value, TimeSpan.Zero);

        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeZoneInfo.Local.GetUtcOffset(value));
    }
}
