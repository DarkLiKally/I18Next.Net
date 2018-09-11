using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net.PolyglotJs
{
    public class PolyglotInterpolator : IInterpolator
    {
        private static readonly Dictionary<string, Func<int, int>> LanguageToTypeMap;

        private static readonly Dictionary<string, Func<int, int>> PluralTypes = new Dictionary<string, Func<int, int>>
        {
            {
                "arabic", n =>
                {
                    // http://www.arabeyes.org/Plural_Forms
                    if (n < 3)
                        return n;
                    if (n % 100 >= 3 && n % 100 <= 10)
                        return 3;

                    return n % 100 >= 11 ? 4 : 5;
                }
            },
            { "chinese", n => 0 },
            { "german", n => n != 1 ? 1 : 0 },
            { "french", n => n > 1 ? 1 : 0 },
            {
                "russian", n =>
                {
                    if (n % 10 == 1 && n % 100 != 11)
                        return 0;

                    return n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2;
                }
            },
            {
                "czech", n =>
                {
                    if (n == 1)
                        return 0;

                    return n >= 2 && n <= 4 ? 1 : 2;
                }
            },
            {
                "polish", n =>
                {
                    if (n == 1)
                        return 0;

                    return n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2;
                }
            },
            { "icelandic", n => n % 10 != 1 || n % 100 == 11 ? 1 : 0 }
        };

        private static readonly Dictionary<string, string[]> PluralTypesToLanguage = new Dictionary<string, string[]>
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

        private static readonly Regex TokenRegex = new Regex(@"%\{(.*?)\}");

        static PolyglotInterpolator()
        {
            if (LanguageToTypeMap != null)
                return;

            LanguageToTypeMap = new Dictionary<string, Func<int, int>>();

            lock (LanguageToTypeMap)
            {
                foreach (var mapping in PluralTypesToLanguage)
                    foreach (var lang in mapping.Value)
                        LanguageToTypeMap.Add(lang, PluralTypes[mapping.Key]);
            }
        }

        public string PluralDelimiter { get; set; } = "||||";

        public bool CanNest(string source)
        {
            return false;
        }

        /// <summary>
        ///     <para>
        ///         Takes a phrase string and transforms it by choosing the correct
        ///         plural form and interpolating it.
        ///         <code>
        ///             transformPhrase('Hello, %{name}!', {name: 'Spike'});
        ///             // "Hello, Spike!"
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The correct plural form is selected if substitutions.smart_count
        ///         is set. You can pass in a number instead of an Object as `substitutions`
        ///         as a shortcut for `smart_count`.
        ///         <code>
        ///             transformPhrase('%{smart_count} new messages |||| 1 new message', {smart_count: 1}, 'en');
        ///             // "1 new message"
        /// 
        ///             transformPhrase('%{smart_count} new messages |||| 1 new message', {smart_count: 2}, 'en');
        ///             // "2 new messages"
        /// 
        ///             transformPhrase('%{smart_count} new messages |||| 1 new message', 5, 'en');
        ///             // "5 new messages"
        ///         </code>
        ///     </para>
        ///     <para>
        ///         You should pass in a third argument, the locale, to specify the correct plural type.
        ///         It defaults to `'en'` with 2 plural forms.
        ///     </para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <param name="language"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
        {
            if (source == null)
                return Task.FromResult((string) null);

            if (language == null)
                language = "en";

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

                    if (pluralIndex < pluralForms.Length)
                        result = pluralForms[pluralTypeFunc(smartCount)].Trim();
                    else
                        result = pluralForms[0];
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
                var expression = match.Groups[0].Value;
                
                if (args == null || !args.ContainsKey(expression))
                {
                    result = result.ReplaceFirst(match.Value, expression);
                    continue;
                }

                result.ReplaceFirst(match.Value, args[expression].ToString());
            }

            return Task.FromResult(result);
        }

        public Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync)
        {
            throw new NotSupportedException("Nesting is not supported by the Polyglot.js format interpolator.");
        }

        private static string GetLanguagePart(string language)
        {
            var index = language.IndexOf('-');

            if (index == -1)
                return language;

            return language.Substring(0, index);
        }
    }
}
