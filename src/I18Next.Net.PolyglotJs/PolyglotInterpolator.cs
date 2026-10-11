using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net.PolyglotJs;

/// <summary>
///     An interpolator for translations in Polyglot.js format: <c>%{name}</c> placeholders and plural forms separated by
///     <c>||||</c>, selected with the <c>smart_count</c> argument. Nesting is not supported.
/// </summary>
public class PolyglotInterpolator : IInterpolator
{
    private static readonly Dictionary<string, Func<int, int>> LanguageToTypeMap;

    private static readonly Dictionary<string, Func<int, int>> PluralTypes = new()
    {
        {
            "arabic", n =>
            {
                // http://www.arabeyes.org/Plural_Forms
                if (n < 3)
                    return n;
                return n % 100 >= 3 && n % 100 <= 10 ? 3 : n % 100 >= 11 ? 4 : 5; }
        },
        { "chinese", n => 0 },
        { "german", n => n != 1 ? 1 : 0 },
        { "french", n => n > 1 ? 1 : 0 },
        {
            "russian", n =>
            {
                return n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2; }
        },
        {
            "czech", n =>
            {
                return n == 1 ? 0 : n >= 2 && n <= 4 ? 1 : 2; }
        },
        {
            "polish", n =>
            {
                return n == 1 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2; }
        },
        { "icelandic", n => n % 10 != 1 || n % 100 == 11 ? 1 : 0 }
    };

    private static readonly Dictionary<string, string[]> PluralTypesToLanguage = new()
    {
        { "arabic", new[] { "ar" } },
        { "chinese", new[] { "id", "ja", "ko", "lo", "ms", "th", "tr", "zh" } },
        { "german", new[] { "fa", "da", "de", "en", "es", "fi", "el", "he", "hu", "it", "nl", "no", "pt", "sv" } },
        { "french", new[] { "fr", "tl", "pt-br" } },
        { "russian", new[] { "hr", "ru", "lt" } },
        { "czech", new[] { "cs", "sk" } },
        { "polish", new[] { "pl" } },
        { "icelandic", new[] { "is" } }
    };

    private static readonly Regex TokenRegex = new(@"%\{(.*?)\}");

    static PolyglotInterpolator()
    {
        if (LanguageToTypeMap != null)
            return;

        LanguageToTypeMap = [];

        lock (LanguageToTypeMap)
        {
            foreach (var mapping in PluralTypesToLanguage)
            {
                foreach (var lang in mapping.Value)
                    LanguageToTypeMap.Add(lang, PluralTypes[mapping.Key]);
            }
        }
    }

    /// <summary>
    ///     The delimiter separating the plural forms of a translation, <c>||||</c> by default.
    /// </summary>
    public string PluralDelimiter { get; set; } = "||||";

    /// <summary>
    ///     Returns <c>false</c> because nesting is not supported.
    /// </summary>
    /// <param name="source">The interpolated translation.</param>
    /// <returns>Always <c>false</c>.</returns>
    public bool CanNest(string source)
    {
        return false;
    }

    /// <summary>
    ///     The formatters applied to the inserted values. All formatters which can format a value are applied in order
    ///     and the last result is used. The format is taken from the argument named after the placeholder with a
    ///     <c>_format</c> suffix.
    /// </summary>
    public List<IFormatter> Formatters { get; } = [];

    /// <summary>
    ///     Chooses the plural form of a translation and inserts the arguments into its placeholders, like the Polyglot.js
    ///     <c>transformPhrase</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the <c>smart_count</c> argument is an <see cref="int" />, the translation is split at
    ///         <see cref="PluralDelimiter" /> and the form is chosen by the Polyglot.js plural rules of the language part,
    ///         e.g. <c>"%{smart_count} car |||| %{smart_count} cars"</c>. Languages without rules and missing forms use
    ///         the first form.
    ///     </para>
    ///     <para>
    ///         Placeholders are replaced by the argument with the name between the delimiters, e.g. the argument
    ///         <c>name</c> for the placeholder <c>%{name}</c>. Placeholders without argument are kept.
    ///     </para>
    /// </remarks>
    /// <param name="source">The translation in Polyglot.js format.</param>
    /// <param name="key">The key of the translation. Ignored.</param>
    /// <param name="language">The language whose plural rules are used, <c>en</c> if <c>null</c>.</param>
    /// <param name="args">The arguments, may be <c>null</c>.</param>
    /// <returns>The interpolated translation or <c>null</c> if <paramref name="source" /> is <c>null</c>.</returns>
    public Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
    {
        if (source == null)
            return Task.FromResult((string)null);

        language ??= "en";

        var result = source;
        var languagePart = GetLanguagePart(language);

        // Select plural form: based on a phrase text that contains `n`
        // plural forms separated by `delimiter`, a `locale`, and a `substitutions.smart_count`,
        // choose the correct plural form. This is only done if `count` is set.
        if ((args?.ContainsKey("smart_count") ?? false) && args["smart_count"] is int smartCount)
        {
            var pluralForms = result.Split(PluralDelimiter);

            if (LanguageToTypeMap.TryGetValue(languagePart, out var pluralTypeFunc))
            {
                var pluralIndex = pluralTypeFunc(smartCount);

                result = pluralIndex < pluralForms.Length ? pluralForms[pluralTypeFunc(smartCount)].Trim() : pluralForms[0];
            }
            else
            {
                result = pluralForms[0];
            }
        }

        // Interpolate: Creates a `RegExp` object for each interpolation placeholder.
        var matches = TokenRegex.Matches(result);

        foreach (Match match in matches)
        {
            var expression = match.Groups[1].Value;

            if (args == null || !args.ContainsKey(expression))
                continue;

            var value = args[expression];
            string resultValue = null;
            string format = null;

            if (args.ContainsKey(expression + "_format"))
                format = args[expression + "_format"].ToString();

            foreach (var formatter in Formatters)
            {
                if (formatter.CanFormat(value, format, language))
                    resultValue = formatter.Format(value, format, language);
            }

            resultValue ??= value.ToString();

            result = result.ReplaceFirst(match.Value, resultValue);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    ///     Not supported, <see cref="CanNest" /> always returns <c>false</c>.
    /// </summary>
    /// <param name="source">The interpolated translation.</param>
    /// <param name="language">The language of the translation.</param>
    /// <param name="args">The arguments of the translation.</param>
    /// <param name="translateAsync">Translates the nested keys.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync)
    {
        throw new NotSupportedException("Nesting is not supported by the Polyglot.js format interpolator.");
    }

    private static string GetLanguagePart(string language)
    {
        var index = language.IndexOf('-');

        return index == -1 ? language : language.Substring(0, index);
    }
}
