using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace I18Next.Net.Plugins
{
    public class DefaultPluralResolver : IPluralResolver
    {
        private static readonly Dictionary<int, Func<int, int>> PluralizationFilters = new Dictionary<int, Func<int, int>>
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
            new PluralizationSet
            {
                Languages = new[]
                {
                    "ach", "ak", "am", "arn", "br", "fil", "gun", "ln", "mfe", "mg", "mi", "oc", "pt", "pt-BR",
                    "tg", "ti", "tr", "uz", "wa"
                },
                Numbers = new[] { 1, 2 },
                Fc = 1
            },
            new PluralizationSet
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
            new PluralizationSet
            {
                Languages = new[]
                {
                    "ay", "bo", "cgg", "fa", "id", "ja", "jbo", "ka", "kk", "km", "ko", "ky", "lo",
                    "ms", "sah", "su", "th", "tt", "ug", "vi", "wo", "zh"
                },
                Numbers = new[] { 1 },
                Fc = 3
            },
            new PluralizationSet
            {
                Languages = new[] { "be", "bs", "dz", "hr", "ru", "sr", "uk" },
                Numbers = new[] { 1, 2, 5 },
                Fc = 4
            },
            new PluralizationSet { Languages = new[] { "ar" }, Numbers = new[] { 0, 1, 2, 3, 11, 100 }, Fc = 5 },
            new PluralizationSet { Languages = new[] { "cs", "sk" }, Numbers = new[] { 1, 2, 5 }, Fc = 6 },
            new PluralizationSet { Languages = new[] { "csb", "pl" }, Numbers = new[] { 1, 2, 5 }, Fc = 7 },
            new PluralizationSet { Languages = new[] { "cy" }, Numbers = new[] { 1, 2, 3, 8 }, Fc = 8 },
            new PluralizationSet { Languages = new[] { "fr" }, Numbers = new[] { 1, 2 }, Fc = 9 },
            new PluralizationSet { Languages = new[] { "ga" }, Numbers = new[] { 1, 2, 3, 7, 11 }, Fc = 10 },
            new PluralizationSet { Languages = new[] { "gd" }, Numbers = new[] { 1, 2, 3, 20 }, Fc = 11 },
            new PluralizationSet { Languages = new[] { "is" }, Numbers = new[] { 1, 2 }, Fc = 12 },
            new PluralizationSet { Languages = new[] { "jv" }, Numbers = new[] { 0, 1 }, Fc = 13 },
            new PluralizationSet { Languages = new[] { "kw" }, Numbers = new[] { 1, 2, 3, 4 }, Fc = 14 },
            new PluralizationSet { Languages = new[] { "lt" }, Numbers = new[] { 1, 2, 10 }, Fc = 15 },
            new PluralizationSet { Languages = new[] { "lv" }, Numbers = new[] { 1, 2, 0 }, Fc = 16 },
            new PluralizationSet { Languages = new[] { "mk" }, Numbers = new[] { 1, 2 }, Fc = 17 },
            new PluralizationSet { Languages = new[] { "mnk" }, Numbers = new[] { 0, 1, 2 }, Fc = 18 },
            new PluralizationSet { Languages = new[] { "mt" }, Numbers = new[] { 1, 2, 11, 20 }, Fc = 19 },
            new PluralizationSet { Languages = new[] { "or" }, Numbers = new[] { 2, 1 }, Fc = 2 },
            new PluralizationSet { Languages = new[] { "ro" }, Numbers = new[] { 1, 2, 20 }, Fc = 20 },
            new PluralizationSet { Languages = new[] { "sl" }, Numbers = new[] { 5, 1, 2, 3 }, Fc = 21 }
        };

        private static readonly ConcurrentDictionary<string, PluralizationRule> Rules;

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
                        Rules.TryAdd(language, new PluralizationRule
                        {
                            Numbers = set.Numbers,
                            Filter = PluralizationFilters[set.Fc]
                        });
                }
            }
        }

        public string PluralSeparator { get; set; } = "_";

        public bool UseLegacyMode { get; set; }

        public bool UseSimplePluralSuffixIfPossible { get; set; } = true;

        public string GetPluralSuffix(string language, int count)
        {
            var rule = GetRule(language);

            if (rule == null)
                return string.Empty;

            var numberIndex = rule.Filter(count);
            var suffixNumber = numberIndex > rule.Numbers.Length ? numberIndex : rule.Numbers[numberIndex];
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

            if (UseLegacyMode)
            {
                if (suffixNumber == 1)
                    return string.Empty;

                if (suffixNumber > 2)
                    return $"_plural_{suffixNumber.ToString()}";

                return $"_{suffix}";
            }

            if (suffix == null)
                return string.Empty;

            return $"{PluralSeparator}{suffix}";
        }

        public bool NeedsPlural(string language)
        {
            var rule = GetRule(language);

            return rule != null && rule.Numbers.Length > 1;
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

        private class PluralizationSet
        {
            public int Fc { get; set; }

            public string[] Languages { get; set; }

            public int[] Numbers { get; set; }
        }
    }
}
