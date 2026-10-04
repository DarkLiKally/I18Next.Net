using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

/// <summary>
///     Translates MomentJS tokens to .Net format tokens and uses this to format DateTime and DateTimeOffset values.
/// </summary>
public class MomentJsFormatter : IFormatter
{
    private static readonly Dictionary<string, string> LocalTokenMap = new()
    {
        { "LT", "t" },
        { "LTS", "T" },
        { "L", "d" },
        { "l", "d" },
        { "LL", "D" },
        { "ll", "D" },
        { "LLL", "G" },
        { "lll", "g" },
        { "LLLL", "F" },
        { "llll", "f" }
    };

    private static readonly Regex LocalTokenRegex = new(@"(\[[^\[]*\])|(\\)?(LTS|LT|LL?L?L?|l{1,4}|\[)");

    private static readonly Dictionary<string, string> TokenMap = new()
    {
        { "M", "M" },
        { "Mo", "-" },
        { "MM", "MM" },
        { "MMM", "MMM" },
        { "MMMM", "MMMM" },
        { "Q", "-" },
        { "Qo", "-" },
        { "D", "d" },
        { "Do", "-" },
        { "DD", "dd" },
        { "DDD", "-" },
        { "DDDo", "-" },
        { "DDDD", "-" },
        { "d", "-" },
        { "do", "-" },
        { "dd", "ddd" },
        { "ddd", "dddd" },
        { "dddd", "ddddd" },
        { "e", "-" },
        { "E", "-" },
        { "w", "-" },
        { "wo", "-" },
        { "ww", "-" },
        { "W", "-" },
        { "Wo", "-" },
        { "WW", "-" },
        { "YY", "yy" },
        { "YYYY", "yyyy" },
        { "Y", "\\Y" },
        { "gg", "\\g\\g" },
        { "gggg", "\\G\\G" },
        { "GG", "\\g\\g" },
        { "GGGG", "\\G\\G\\G\\G" },
        { "A", "tt" },
        { "a", "-" },
        { "H", "H" },
        { "HH", "HH" },
        { "h", "h" },
        { "hh", "hh" },
        { "k", "-" },
        { "kk", "-" },
        { "m", "m" },
        { "mm", "mm" },
        { "s", "s" },
        { "ss", "ss" },
        { "S", "f" },
        { "SS", "ff" },
        { "SSS", "fff" },
        { "SSSS", "ffff" },
        { "SSSSS", "fffff" },
        { "SSSSSS", "ffffff" },
        { "SSSSSSS", "fffffff" },
        { "SSSSSSSS", "-" },
        { "SSSSSSSSS", "-" },
        { "z", "-" },
        { "zz", "-" },
        { "Z", "zzz" },
        { "ZZ", "-" },
        { "X", "-" },
        { "x", "-" }
    };

    private static readonly Regex TokenRegex =
        new(
            @"(\[[^\[]*\])|(\\)?([Hh]mm(ss)?|Mo|MM?M?M?|Do|DDDo|DD?D?D?|ddd?d?|do?|w[o|w]?|W[o|W]?|Qo?|YYYYYY|YYYYY|YYYY|YY|gg(ggg?)?|GG(GGG?)?|e|E|a|A|hh?|HH?|kk?|mm?|ss?|S{1,9}|x|X|zz?|ZZ?|.)");

    public bool CanFormat(object value, string format, string language)
    {
        return (value is DateTime || value is DateTimeOffset) && !IntlFormatter.IsFormatName(format);
    }

    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var culture = CultureInfo.GetCultureInfo(language);

        if (value is DateTime dt)
            return ReplaceTokens(dt, format, culture);

        return value is DateTimeOffset dto ? ReplaceTokens(dto, format, culture) : value.ToString();
    }

    private static string AddOrdinal(int num)
    {
        if (num <= 0)
            return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        return (num % 10) switch
        {
            1 => num + "st",
            2 => num + "nd",
            3 => num + "rd",
            _ => num + "th",
        };
    }

    private static int GetQuarter(int month)
    {
        return (month + 2) / 3;
    }

    private string GetSpecialTokenValue(DateTimeOffset value, string token, CultureInfo culture)
    {
        return token switch
        {
            "Mo" => AddOrdinal(value.Month),
            "Q" => GetQuarter(value.Month).ToString(),
            "Qo" => AddOrdinal(GetQuarter(value.Month)),
            "Do" => AddOrdinal(value.Day),
            "DDD" => value.DayOfYear.ToString(),
            "DDDo" => AddOrdinal(value.DayOfYear),
            "DDDD" => value.DayOfYear.ToString("000"),
            "d" => ((int)value.DayOfWeek).ToString(),
            "do" => AddOrdinal((int)value.DayOfWeek),
            "e" => ((int)value.DayOfWeek).ToString(),
            "E" => ((int)value.DayOfWeek + 1).ToString(),
            "w" or "wo" or "ww" or "W" or "Wo" or "WW" => GetWeekTokenValue(value, token, culture),
            "a" => value.ToString("tt", culture).ToLower(),
            "k" => (value.Hour + 1).ToString(),
            "kk" => (value.Hour + 1).ToString("00"),
            "SSSSSSSS" => value.ToString("fffffff00", culture),
            "SSSSSSSSS" => value.ToString("fffffff000", culture),
            "z" or "zz" => TimeZoneData.GetFirstForOffset(value.Offset).Abbreviation,
            "ZZ" => value.ToString("zzz", culture).Replace(":", ""),
            "X" => value.ToUnixTimeSeconds().ToString(),
            "x" => value.ToUnixTimeMilliseconds().ToString(),
            _ => token,
        };
    }

    private string GetWeekTokenValue(DateTimeOffset value, string token, CultureInfo culture)
    {
        switch (token)
        {
            case "w":
            case "wo":
            case "ww":
                var week = culture.Calendar.GetWeekOfYear(value.DateTime, CalendarWeekRule.FirstDay, culture.DateTimeFormat.FirstDayOfWeek);

                return token switch
                {
                    "ww" => week.ToString("00"),
                    "wo" => AddOrdinal(week),
                    _ => week.ToString(),
                };
            case "W":
            case "Wo":
            case "WW":
                var weekIso = culture.Calendar.GetWeekOfYear(value.DateTime, culture.DateTimeFormat.CalendarWeekRule,
                    culture.DateTimeFormat.FirstDayOfWeek);

                return token switch
                {
                    "WW" => weekIso.ToString("00"),
                    "Wo" => AddOrdinal(weekIso),
                    _ => weekIso.ToString(),
                };
        }

        return token;
    }

    private string ReplaceTokens(DateTimeOffset value, string format, CultureInfo culture)
    {
        var lastPosition = 0;
        Match localMatch;
        while ((localMatch = LocalTokenRegex.Match(format, lastPosition)).Success)
        {
            lastPosition = localMatch.Index;

            if (localMatch.Value.StartsWith("["))
            {
                lastPosition += localMatch.Length;
                continue;
            }

            if (localMatch.Value.StartsWith("\\"))
            {
                if (localMatch.Value.StartsWith("\\["))
                {
                    lastPosition += localMatch.Length;
                    continue;
                }

                var tokenValue = localMatch.Value.Substring(1);
                format = SwapStringPart(format, localMatch.Index, localMatch.Length, $"[{tokenValue}]");
                lastPosition += tokenValue.Length + 2;
                continue;
            }

            if (LocalTokenMap.TryGetValue(localMatch.Value, out var newToken))
            {
                var tokenValue = value.ToString(newToken, culture);
                format = SwapStringPart(format, localMatch.Index, localMatch.Length, $"[{tokenValue}]");
                lastPosition += tokenValue.Length + 2;
            }
        }

        var matches = TokenRegex.Matches(format);
        var output = string.Empty;

        foreach (Match match in matches)
        {
            if (match.Value.StartsWith("["))
            {
                output += match.Value.Substring(1, match.Value.Length - 2);
                continue;
            }

            if (match.Value.StartsWith("\\"))
            {
                output += match.Value.Substring(1);
                continue;
            }

            if (TokenMap.TryGetValue(match.Value, out var newToken))
            {
                if (newToken == "-")
                {
                    output += GetSpecialTokenValue(value, match.Value, culture);
                    continue;
                }

                output += value.ToString(newToken + " ", culture).TrimEnd();
                continue;
            }

            output += match.Value;
        }

        return output;
    }

    private string SwapStringPart(string source, int index, int length, string newPart)
    {
        var before = source.Substring(0, index);
        var after = source.Substring(index + length);

        return $"{before}{newPart}{after}";
    }
}
