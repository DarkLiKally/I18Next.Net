using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace I18Next.Net.Plugins;

public enum JsonFormat
{
    Version1 = 1,
    Version2 = 2,
    Version3 = 3,
    Version4 = 4
}

public class DefaultPluralResolver : IPluralResolver
{
    private const string Zero = "zero";
    private const string One = "one";
    private const string Two = "two";
    private const string Few = "few";
    private const string Many = "many";
    private const string Other = "other";

    private static readonly Dictionary<int, Func<int, int>> PluralizationFilters = new()
    {
            // @formatter:off
            { 1, n => n > 1 ? 1 : 0 },
            { 2, n => n != 1 ? 1 : 0 },
            { 3, n => 0 },
            { 4, n => n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2 },
            { 5, n => n == 0 ? 0 : n == 1 ? 1 : n == 2 ? 2 : n % 100 >= 3 && n % 100 <= 10 ? 3 : n % 100 >= 11 ? 4 : 5 },
            { 6, n => n == 1 ? 0 : n >= 2 && n <= 4 ? 1 : 2 },
            { 7, n => n == 1 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2 },
            { 8, n => n == 1 ? 0 : n == 2 ? 1 : n != 8 && n != 11 ? 2 : 3 },
            { 9, n => n >= 2 ? 1 : 0 },
            { 10, n => n == 1 ? 0 : n == 2 ? 1 : n < 7 ? 2 : n < 11 ? 3 : 4 },
            { 11, n => n == 1 || n == 11 ? 0 : n == 2 || n == 12 ? 1 : n > 2 && n < 20 ? 2 : 3 },
            { 12, n => n % 10 != 1 || n % 100 == 11 ? 1 : 0 },
            { 13, n => n != 0 ? 1 : 0 },
            { 14, n => n == 1 ? 0 : n == 2 ? 1 : n == 3 ? 2 : 3 },
            { 15, n => n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2 },
            { 16, n => n % 10 == 1 && n % 100 != 11 ? 0 : n != 0 ? 1 : 2 },
            { 17, n => n == 1 || n % 10 == 1 ? 0 : 1 },
            { 18, n => n == 0 ? 0 : n == 1 ? 1 : 2 },
            { 19, n => n == 1 ? 0 : n == 0 || n % 100 > 1 && n % 100 < 11 ? 1 : n % 100 > 10 && n % 100 < 20 ? 2 : 3 },
            { 20, n => n == 1 ? 0 : n == 0 || n % 100 > 0 && n % 100 < 20 ? 1 : 2 },
            { 21, n => n % 100 == 1 ? 1 : n % 100 == 2 ? 2 : n % 100 == 3 || n % 100 == 4 ? 3 : 0 }
        // @formatter:on
    };

    private static readonly PluralizationSet[] PluralizationSets =
    {
        new()
        {
            Languages = new[]
            {
                "ach", "ak", "am", "arn", "br", "fil", "gun", "ln", "mfe", "mg", "mi", "oc", "pt", "pt-BR",
                "tg", "ti", "tr", "uz", "wa"
            },
            Numbers = new[] { 1, 2 },
            Fc = 1
        },
        new()
        {
            Languages = new[]
            {
                "af", "an", "ast", "az", "bg", "bn", "ca", "da", "de", "dev", "el", "en",
                "eo", "es", "et", "eu", "fi", "fo", "fur", "fy", "gl", "gu", "ha", "he", "hi",
                "hu", "hy", "ia", "it", "kn", "ku", "lb", "mai", "ml", "mn", "mr", "nah", "nap", "nb",
                "ne", "nl", "nn", "no", "nso", "pa", "pap", "pms", "ps", "pt-PT", "rm", "sco",
                "se", "si", "so", "son", "sq", "sv", "sw", "ta", "te", "tk", "ur", "yo"
            },
            Numbers = new[] { 1, 2 },
            Fc = 2
        },
        new()
        {
            Languages = new[]
            {
                "ay", "bo", "cgg", "fa", "id", "ja", "jbo", "ka", "kk", "km", "ko", "ky", "lo",
                "ms", "sah", "su", "th", "tt", "ug", "vi", "wo", "zh"
            },
            Numbers = new[] { 1 },
            Fc = 3
        },
        new()
        {
            Languages = new[] { "be", "bs", "dz", "hr", "ru", "sr", "uk" },
            Numbers = new[] { 1, 2, 5 },
            Fc = 4
        },
        new() { Languages = new[] { "ar" }, Numbers = new[] { 0, 1, 2, 3, 11, 100 }, Fc = 5 },
        new() { Languages = new[] { "cs", "sk" }, Numbers = new[] { 1, 2, 5 }, Fc = 6 },
        new() { Languages = new[] { "csb", "pl" }, Numbers = new[] { 1, 2, 5 }, Fc = 7 },
        new() { Languages = new[] { "cy" }, Numbers = new[] { 1, 2, 3, 8 }, Fc = 8 },
        new() { Languages = new[] { "fr" }, Numbers = new[] { 1, 2 }, Fc = 9 },
        new() { Languages = new[] { "ga" }, Numbers = new[] { 1, 2, 3, 7, 11 }, Fc = 10 },
        new() { Languages = new[] { "gd" }, Numbers = new[] { 1, 2, 3, 20 }, Fc = 11 },
        new() { Languages = new[] { "is" }, Numbers = new[] { 1, 2 }, Fc = 12 },
        new() { Languages = new[] { "jv" }, Numbers = new[] { 0, 1 }, Fc = 13 },
        new() { Languages = new[] { "kw" }, Numbers = new[] { 1, 2, 3, 4 }, Fc = 14 },
        new() { Languages = new[] { "lt" }, Numbers = new[] { 1, 2, 10 }, Fc = 15 },
        new() { Languages = new[] { "lv" }, Numbers = new[] { 1, 2, 0 }, Fc = 16 },
        new() { Languages = new[] { "mk" }, Numbers = new[] { 1, 2 }, Fc = 17 },
        new() { Languages = new[] { "mnk" }, Numbers = new[] { 0, 1, 2 }, Fc = 18 },
        new() { Languages = new[] { "mt" }, Numbers = new[] { 1, 2, 11, 20 }, Fc = 19 },
        new() { Languages = new[] { "or" }, Numbers = new[] { 2, 1 }, Fc = 2 },
        new() { Languages = new[] { "ro" }, Numbers = new[] { 1, 2, 20 }, Fc = 20 },
        new() { Languages = new[] { "sl" }, Numbers = new[] { 5, 1, 2, 3 }, Fc = 21 }
    };

    private static readonly PluralCategorySet[] PluralCategorySets =
    {
        // @formatter:off
        new()
        {
            Languages = new[]
            {
                "bm", "bo", "dz", "hnj", "id", "ig", "ii", "in", "ja", "jbo", "jv", "jw", "kde", "kea", "km", "ko", "lkt",
                "lo", "ms", "my", "nqo", "osa", "sah", "ses", "sg", "su", "th", "to", "tpi", "vi", "wo", "yo", "yue", "zh"
            },
            Filter = n => Other
        },
        new()
        {
            Languages = new[]
            {
                "ak", "am", "as", "bho", "bn", "csw", "doi", "fa", "ff", "gu", "guw", "hi", "hy", "kab", "kn", "ln", "mg",
                "nso", "pa", "pcm", "si", "ti", "wa", "zu"
            },
            Filter = n => n <= 1 ? One : Other
        },
        new()
        {
            Languages = new[]
            {
                "af", "an", "asa", "ast", "az", "bal", "bem", "bez", "bg", "brx", "ce", "cgg", "chr", "ckb", "da", "de",
                "dev", "dv", "ee", "el", "en", "eo", "et", "eu", "fi", "fo", "fur", "fy", "gl", "gsw", "ha", "haw", "hu",
                "ia", "io", "ji", "jgo", "jmc", "ka", "kaj", "kcg", "kk", "kkj", "kl", "ks", "ksb", "ku", "ky", "lb", "lg",
                "lij", "mas", "mgo", "ml", "mn", "mr", "nah", "nb", "nd", "ne", "nl", "nn", "nnh", "no", "nr", "ny", "nyn",
                "om", "or", "os", "pap", "ps", "rm", "rof", "rwk", "saq", "sc", "sd", "sdh", "seh", "sn", "so", "sq", "ss",
                "ssy", "st", "sv", "sw", "syr", "ta", "te", "teo", "tig", "tk", "tn", "tr", "ts", "ug", "ur", "uz", "ve",
                "vo", "vun", "wae", "xh", "xog", "yi"
            },
            Filter = n => n == 1 ? One : Other
        },
        new() { Languages = new[] { "tzm" }, Filter = n => n <= 1 || n >= 11 && n <= 99 ? One : Other },
        new() { Languages = new[] { "is", "mk" }, Filter = n => n % 10 == 1 && n % 100 != 11 ? One : Other },
        new() { Languages = new[] { "ceb", "fil", "tl" }, Filter = n => n % 10 != 4 && n % 10 != 6 && n % 10 != 9 ? One : Other },
        new() { Languages = new[] { "lv", "prg" }, Filter = n => n % 10 == 0 || n % 100 >= 11 && n % 100 <= 19 ? Zero : n % 10 == 1 && n % 100 != 11 ? One : Other },
        new() { Languages = new[] { "ksh", "lag" }, Filter = n => n == 0 ? Zero : n == 1 ? One : Other },
        new() { Languages = new[] { "he", "iu", "naq", "sat", "se", "sma", "smi", "smj", "smn", "sms" }, Filter = n => n == 1 ? One : n == 2 ? Two : Other },
        new() { Languages = new[] { "shi" }, Filter = n => n <= 1 ? One : n <= 10 ? Few : Other },
        new() { Languages = new[] { "mo", "ro" }, Filter = n => n == 1 ? One : n == 0 || n % 100 >= 1 && n % 100 <= 19 ? Few : Other },
        new() { Languages = new[] { "bs", "hr", "sh", "sr" }, Filter = n => n % 10 == 1 && n % 100 != 11 ? One : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 12 || n % 100 > 14) ? Few : Other },
        new() { Languages = new[] { "gd" }, Filter = n => n == 1 || n == 11 ? One : n == 2 || n == 12 ? Two : n >= 3 && n <= 10 || n >= 13 && n <= 19 ? Few : Other },
        new() { Languages = new[] { "dsb", "hsb", "sl" }, Filter = n => n % 100 == 1 ? One : n % 100 == 2 ? Two : n % 100 == 3 || n % 100 == 4 ? Few : Other },
        new() { Languages = new[] { "cs", "sk" }, Filter = n => n == 1 ? One : n >= 2 && n <= 4 ? Few : Other },
        new() { Languages = new[] { "pl" }, Filter = n => n == 1 ? One : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 12 || n % 100 > 14) ? Few : Many },
        new() { Languages = new[] { "be", "ru", "uk" }, Filter = n => n % 10 == 1 && n % 100 != 11 ? One : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 12 || n % 100 > 14) ? Few : Many },
        new() { Languages = new[] { "lt" }, Filter = n => n % 10 == 1 && (n % 100 < 11 || n % 100 > 19) ? One : n % 10 >= 2 && (n % 100 < 11 || n % 100 > 19) ? Few : Other },
        new()
        {
            Languages = new[] { "br" },
            Filter = n => n % 10 == 1 && n % 100 != 11 && n % 100 != 71 && n % 100 != 91 ? One
                : n % 10 == 2 && n % 100 != 12 && n % 100 != 72 && n % 100 != 92 ? Two
                : (n % 10 == 3 || n % 10 == 4 || n % 10 == 9) && (n % 100 < 10 || n % 100 > 19) && (n % 100 < 70 || n % 100 > 79) && (n % 100 < 90 || n % 100 > 99) ? Few
                : n != 0 && n % 1000000 == 0 ? Many
                : Other
        },
        new() { Languages = new[] { "mt" }, Filter = n => n == 1 ? One : n == 2 ? Two : n == 0 || n % 100 >= 3 && n % 100 <= 10 ? Few : n % 100 >= 11 && n % 100 <= 19 ? Many : Other },
        new() { Languages = new[] { "ga" }, Filter = n => n == 1 ? One : n == 2 ? Two : n >= 3 && n <= 6 ? Few : n >= 7 && n <= 10 ? Many : Other },
        new() { Languages = new[] { "gv" }, Filter = n => n % 10 == 1 ? One : n % 10 == 2 ? Two : n % 20 == 0 ? Few : Other },
        new()
        {
            Languages = new[] { "kw" },
            Filter = n => n == 0 ? Zero
                : n == 1 ? One
                : n % 100 == 2 || n % 100 == 22 || n % 100 == 42 || n % 100 == 62 || n % 100 == 82
                  || n % 1000 == 0 && (n % 100000 >= 1000 && n % 100000 <= 20000 || n % 100000 == 40000 || n % 100000 == 60000 || n % 100000 == 80000)
                  || n != 0 && n % 1000000 == 100000 ? Two
                : n % 100 == 3 || n % 100 == 23 || n % 100 == 43 || n % 100 == 63 || n % 100 == 83 ? Few
                : n % 100 == 1 || n % 100 == 21 || n % 100 == 41 || n % 100 == 61 || n % 100 == 81 ? Many
                : Other
        },
        new() { Languages = new[] { "ar", "ars" }, Filter = n => n == 0 ? Zero : n == 1 ? One : n == 2 ? Two : n % 100 >= 3 && n % 100 <= 10 ? Few : n % 100 >= 11 ? Many : Other },
        new() { Languages = new[] { "cy" }, Filter = n => n == 0 ? Zero : n == 1 ? One : n == 2 ? Two : n == 3 ? Few : n == 6 ? Many : Other },
        new() { Languages = new[] { "fr", "pt" }, Filter = n => n <= 1 ? One : n % 1000000 == 0 ? Many : Other },
        new() { Languages = new[] { "ca", "es", "it", "lld", "pt-PT", "scn", "vec" }, Filter = n => n == 1 ? One : n != 0 && n % 1000000 == 0 ? Many : Other }
        // @formatter:on
    };

    private static readonly PluralCategorySet[] OrdinalCategorySets =
    {
        // @formatter:off
        new() { Languages = new[] { "en", "dev" }, Filter = n => n % 10 == 1 && n % 100 != 11 ? One : n % 10 == 2 && n % 100 != 12 ? Two : n % 10 == 3 && n % 100 != 13 ? Few : Other },
        new() { Languages = new[] { "bal", "fil", "fr", "ga", "hy", "lo", "mo", "ms", "ro", "tl", "vi" }, Filter = n => n == 1 ? One : Other },
        new() { Languages = new[] { "hu" }, Filter = n => n == 1 || n == 5 ? One : Other },
        new() { Languages = new[] { "ne" }, Filter = n => n >= 1 && n <= 4 ? One : Other },
        new() { Languages = new[] { "sv" }, Filter = n => (n % 10 == 1 || n % 10 == 2) && n % 100 != 11 && n % 100 != 12 ? One : Other },
        new() { Languages = new[] { "it", "lld", "sc", "scn", "vec" }, Filter = n => n == 11 || n == 8 || n == 80 || n == 800 ? Many : Other },
        new() { Languages = new[] { "kk" }, Filter = n => n % 10 == 6 || n % 10 == 9 || n % 10 == 0 && n != 0 ? Many : Other },
        new() { Languages = new[] { "ka" }, Filter = n => n == 1 ? One : n == 0 || n % 100 >= 2 && n % 100 <= 20 || n % 100 == 40 || n % 100 == 60 || n % 100 == 80 ? Many : Other },
        new() { Languages = new[] { "sq" }, Filter = n => n == 1 ? One : n % 10 == 4 && n % 100 != 14 ? Many : Other },
        new() { Languages = new[] { "uk" }, Filter = n => n % 10 == 3 && n % 100 != 13 ? Few : Other },
        new() { Languages = new[] { "be" }, Filter = n => (n % 10 == 2 || n % 10 == 3) && n % 100 != 12 && n % 100 != 13 ? Few : Other },
        new() { Languages = new[] { "tk" }, Filter = n => n % 10 == 6 || n % 10 == 9 || n == 10 ? Few : Other },
        new() { Languages = new[] { "mk" }, Filter = n => n % 10 == 1 && n % 100 != 11 ? One : n % 10 == 2 && n % 100 != 12 ? Two : (n % 10 == 7 || n % 10 == 8) && n % 100 != 17 && n % 100 != 18 ? Many : Other },
        new() { Languages = new[] { "ca" }, Filter = n => n == 1 || n == 3 ? One : n == 2 ? Two : n == 4 ? Few : Other },
        new() { Languages = new[] { "mr" }, Filter = n => n == 1 ? One : n == 2 || n == 3 ? Two : n == 4 ? Few : Other },
        new() { Languages = new[] { "gu", "hi" }, Filter = n => n == 1 ? One : n == 2 || n == 3 ? Two : n == 4 ? Few : n == 6 ? Many : Other },
        new() { Languages = new[] { "as", "bn" }, Filter = n => n == 1 || n == 5 || n >= 7 && n <= 10 ? One : n == 2 || n == 3 ? Two : n == 4 ? Few : n == 6 ? Many : Other },
        new() { Languages = new[] { "or" }, Filter = n => n == 1 || n == 5 || n >= 7 && n <= 9 ? One : n == 2 || n == 3 ? Two : n == 4 ? Few : n == 6 ? Many : Other },
        new() { Languages = new[] { "gd" }, Filter = n => n == 1 || n == 11 ? One : n == 2 || n == 12 ? Two : n == 3 || n == 13 ? Few : Other },
        new() { Languages = new[] { "cy" }, Filter = n => n == 0 || n == 7 || n == 8 || n == 9 ? Zero : n == 1 ? One : n == 2 ? Two : n == 3 || n == 4 ? Few : n == 5 || n == 6 ? Many : Other },
        new()
        {
            Languages = new[] { "az" },
            Filter = n => n % 10 == 1 || n % 10 == 2 || n % 10 == 5 || n % 10 == 7 || n % 10 == 8 || n % 100 == 20 || n % 100 == 50 || n % 100 == 70 || n % 100 == 80 ? One
                : n % 10 == 3 || n % 10 == 4 || n % 1000 >= 100 && n % 1000 <= 900 && n % 100 == 0 ? Few
                : n == 0 || n % 10 == 6 || n % 100 == 40 || n % 100 == 60 || n % 100 == 90 ? Many
                : Other
        }
        // @formatter:on
    };

    private static readonly ConcurrentDictionary<string, PluralizationRule> Rules;

    private static readonly Dictionary<string, Func<long, string>> CategoryRules;

    private static readonly Dictionary<string, Func<long, string>> OrdinalCategoryRules;

    static DefaultPluralResolver()
    {
        lock (PluralizationSets)
        {
            if (Rules != null)
                return;

            Rules = new ConcurrentDictionary<string, PluralizationRule>();

            foreach (var set in PluralizationSets)
            {
                foreach (var language in set.Languages)
                {
                    Rules.TryAdd(language, new PluralizationRule
                    {
                        Numbers = set.Numbers,
                        Filter = PluralizationFilters[set.Fc]
                    });
                }
            }

            CategoryRules = new Dictionary<string, Func<long, string>>();

            foreach (var set in PluralCategorySets)
            {
                foreach (var language in set.Languages)
                    CategoryRules[language] = set.Filter;
            }

            OrdinalCategoryRules = new Dictionary<string, Func<long, string>>();

            foreach (var set in OrdinalCategorySets)
            {
                foreach (var language in set.Languages)
                    OrdinalCategoryRules[language] = set.Filter;
            }
        }
    }

    public string PluralSeparator { get; set; } = "_";

    public JsonFormat JsonFormatVersion { get; set; } = JsonFormat.Version3;

    public bool UseSimplePluralSuffixIfPossible { get; set; } = true;

    public string GetPluralSuffix(string language, int count)
    {
        if (JsonFormatVersion == JsonFormat.Version4)
            return $"{PluralSeparator}{GetPluralCategory(language, count)}";

        var rule = GetRule(language);

        if (rule == null)
            return string.Empty;

        var numberIndex = rule.Filter(count == int.MinValue ? int.MaxValue : Math.Abs(count));
        var suffixNumber = numberIndex >= rule.Numbers.Length ? numberIndex : rule.Numbers[numberIndex];
        string suffix;

        if (UseSimplePluralSuffixIfPossible && rule.Numbers.Length == 2 && rule.Numbers[0] == 1)
        {
            if (suffixNumber == 2)
                suffix = "plural";
            else if (suffixNumber == 1)
                suffix = null;
            else
                suffix = suffixNumber.ToString();
        }
        else
        {
            suffix = suffixNumber.ToString();
        }

        switch (JsonFormatVersion)
        {
            case JsonFormat.Version1:
                if (suffixNumber == 1)
                    return string.Empty;

                if (suffix == "plural")
                    return "_plural";

                return $"_plural_{suffixNumber.ToString()}";

            case JsonFormat.Version2:
                if (rule.Numbers.Length == 1 || suffix == null)
                    return string.Empty;

                return $"{PluralSeparator}{suffix}";    

            default:
                if (UseSimplePluralSuffixIfPossible && rule.Numbers.Length == 2 && rule.Numbers[0] == 1)
                    return suffix == null ? string.Empty : $"{PluralSeparator}{suffix}";
                else
                    return $"{PluralSeparator}{numberIndex}";
        }
    }

    public bool NeedsPlural(string language)
    {
        if (JsonFormatVersion >= JsonFormat.Version3)
            return true;

        var rule = GetRule(language);

        return rule != null && rule.Numbers.Length > 1;
    }

    /// <summary>
    ///     Gets the CLDR plural category (zero, one, two, few, many or other) of the given count for the given language.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <param name="count">Count of items.</param>
    /// <returns>The plural category. Falls back to "other" for unknown languages.</returns>
    public static string GetPluralCategory(string language, int count)
    {
        var n = Math.Abs((long) count);

        if (CategoryRules.TryGetValue(language, out var rule) || CategoryRules.TryGetValue(GetLanguagePart(language), out rule))
            return rule(n);

        return Other;
    }

    /// <summary>
    ///     Gets the suffix for ordinal plurals (e.g. "_ordinal_one") of the given count for the given language.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <param name="count">Count of items.</param>
    /// <returns>Suffix to be used to look for ordinal plural handling.</returns>
    public string GetOrdinalPluralSuffix(string language, int count)
    {
        return $"{PluralSeparator}ordinal{PluralSeparator}{GetOrdinalPluralCategory(language, count)}";
    }

    /// <summary>
    ///     Gets the CLDR ordinal plural category (zero, one, two, few, many or other) of the given count for the given language.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <param name="count">Count of items.</param>
    /// <returns>The ordinal plural category. Falls back to "other" for unknown languages.</returns>
    public static string GetOrdinalPluralCategory(string language, int count)
    {
        var n = Math.Abs((long) count);

        if (OrdinalCategoryRules.TryGetValue(language, out var rule) || OrdinalCategoryRules.TryGetValue(GetLanguagePart(language), out rule))
            return rule(n);

        return Other;
    }

    private static string GetLanguagePart(string language)
    {
        var index = language.IndexOf('-');

        if (index == -1)
            return language;

        return language.Substring(0, index);
    }

    private static PluralizationRule GetRule(string language)
    {
        if (Rules.TryGetValue(language, out var rule))
            return rule;

        var languagePart = GetLanguagePart(language);

        if (Rules.TryGetValue(languagePart, out rule))
            return rule;

        return null;
    }

    private class PluralizationRule
    {
        public Func<int, int> Filter { get; set; }

        public int[] Numbers { get; set; }
    }

    private class PluralCategorySet
    {
        public Func<long, string> Filter { get; set; }

        public string[] Languages { get; set; }
    }

    private class PluralizationSet
    {
        public int Fc { get; set; }

        public string[] Languages { get; set; }

        public int[] Numbers { get; set; }
    }
}
