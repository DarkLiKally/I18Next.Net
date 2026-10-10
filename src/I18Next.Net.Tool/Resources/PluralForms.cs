using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using I18Next.Net.Plugins;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     The i18next v4 plural keys (<c>key_one</c>, <c>key_ordinal_few</c>) of a language based on its CLDR plural categories.
/// </summary>
internal static class PluralForms
{
    public const string Separator = "_";

    public const string Other = "other";

    public static readonly string[] Categories = ["zero", "one", "two", "few", "many", Other];

    private static readonly long[] IntegerSamples = Enumerable.Range(0, 201).Select(n => (long)n)
        .Concat([1000, 10000, 100000, 1000000, 2000000, 10000000, 100000000])
        .ToArray();

    private static readonly decimal[] DecimalSamples = [0.0m, 0.1m, 0.5m, 1.0m, 1.5m, 2.0m, 2.5m, 3.5m, 5.5m, 10.5m, 100.5m, 1000.5m];

    private static readonly ConcurrentDictionary<(string Language, bool Ordinal), IReadOnlyList<string>> CategoryCache = new();

    /// <summary>
    ///     Gets the plural categories a language uses, in the order zero, one, two, few, many, other.
    /// </summary>
    public static IReadOnlyList<string> GetCategories(string language, bool ordinal = false)
    {
        return CategoryCache.GetOrAdd((language, ordinal), key =>
        {
            var found = new HashSet<string>(StringComparer.Ordinal) { Other };

            foreach (var sample in IntegerSamples)
            {
                found.Add(key.Ordinal
                    ? DefaultPluralResolver.GetOrdinalPluralCategory(key.Language, (int)sample)
                    : DefaultPluralResolver.GetPluralCategory(key.Language, (int)sample));
            }

            if (!key.Ordinal)
            {
                foreach (var sample in DecimalSamples)
                    found.Add(DefaultPluralResolver.GetPluralCategory(key.Language, sample));
            }

            return Categories.Where(found.Contains).ToList();
        });
    }

    public static string GetSuffix(string category, bool ordinal)
    {
        return ordinal ? Separator + "ordinal" + Separator + category : Separator + category;
    }

    public static IEnumerable<string> GetKeys(string baseKey, string language, bool ordinal)
    {
        return GetCategories(language, ordinal).Select(c => baseKey + GetSuffix(c, ordinal));
    }

    /// <summary>
    ///     Splits a plural key into its base key and category. A key only counts as plural key when the <c>other</c> form
    ///     of its base key exists too.
    /// </summary>
    public static bool TryParse(string key, Func<string, bool> exists, out PluralKey pluralKey)
    {
        foreach (var ordinal in new[] { true, false })
        {
            foreach (var category in Categories)
            {
                var suffix = GetSuffix(category, ordinal);

                if (key.Length <= suffix.Length || !key.EndsWith(suffix, StringComparison.Ordinal))
                    continue;

                var baseKey = key.Substring(0, key.Length - suffix.Length);

                if (category != Other && !exists(baseKey + GetSuffix(Other, ordinal)))
                    continue;

                pluralKey = new PluralKey(baseKey, category, ordinal);
                return true;
            }
        }

        pluralKey = null;
        return false;
    }
}

internal sealed class PluralKey(string baseKey, string category, bool ordinal)
{
    public string BaseKey { get; } = baseKey;

    public string Category { get; } = category;

    public bool Ordinal { get; } = ordinal;

    public string GetKey(string category)
    {
        return BaseKey + PluralForms.GetSuffix(category, Ordinal);
    }
}
