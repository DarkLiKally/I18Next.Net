using System;

using I18Next.Net.Plugins;

using UnityEngine;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     Detects the language of the operating system from <see cref="Application.systemLanguage" />.
    /// </summary>
    public class SystemLanguageDetector : ILanguageDetector
    {
        /// <summary>
        ///     Creates a detector which falls back to the given language.
        /// </summary>
        /// <param name="fallbackLanguage">The language used when the language of the operating system is unknown.</param>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="fallbackLanguage" /> is <c>null</c>, empty or whitespace.
        /// </exception>
        public SystemLanguageDetector(string fallbackLanguage = "en")
        {
            if (string.IsNullOrWhiteSpace(fallbackLanguage))
                throw new ArgumentNullException(nameof(fallbackLanguage));

            FallbackLanguage = fallbackLanguage;
        }

        /// <summary>
        ///     The language used when the language of the operating system is unknown.
        /// </summary>
        public string FallbackLanguage { get; set; }

        /// <summary>
        ///     Detects the language of the operating system.
        /// </summary>
        /// <returns>
        ///     The language code of <see cref="Application.systemLanguage" /> or the <see cref="FallbackLanguage" /> if the
        ///     language is unknown.
        /// </returns>
        public string GetLanguage()
        {
            return GetLanguageCode(Application.systemLanguage) ?? FallbackLanguage;
        }

        /// <summary>
        ///     Gets the language code of a system language, e.g. <c>de</c> for <see cref="SystemLanguage.German" /> or
        ///     <c>zh-Hans</c> for <see cref="SystemLanguage.ChineseSimplified" />.
        /// </summary>
        /// <param name="language">The system language.</param>
        /// <returns>
        ///     The language code or <c>null</c> for <see cref="SystemLanguage.Unknown" /> and unmapped languages.
        /// </returns>
        public static string GetLanguageCode(SystemLanguage language)
        {
            switch (language.ToString())
            {
                case "Afrikaans":
                    return "af";
                case "Arabic":
                    return "ar";
                case "Basque":
                    return "eu";
                case "Belarusian":
                    return "be";
                case "Bulgarian":
                    return "bg";
                case "Catalan":
                    return "ca";
                case "Chinese":
                    return "zh";
                case "ChineseSimplified":
                    return "zh-Hans";
                case "ChineseTraditional":
                    return "zh-Hant";
                case "Czech":
                    return "cs";
                case "Danish":
                    return "da";
                case "Dutch":
                    return "nl";
                case "English":
                    return "en";
                case "Estonian":
                    return "et";
                case "Faroese":
                    return "fo";
                case "Finnish":
                    return "fi";
                case "French":
                    return "fr";
                case "German":
                    return "de";
                case "Greek":
                    return "el";
                case "Hebrew":
                    return "he";
                case "Hindi":
                    return "hi";
                case "Hungarian":
                case "Hugarian":
                    return "hu";
                case "Icelandic":
                    return "is";
                case "Indonesian":
                    return "id";
                case "Italian":
                    return "it";
                case "Japanese":
                    return "ja";
                case "Korean":
                    return "ko";
                case "Latvian":
                    return "lv";
                case "Lithuanian":
                    return "lt";
                case "Norwegian":
                    return "nb";
                case "Polish":
                    return "pl";
                case "Portuguese":
                    return "pt";
                case "Romanian":
                    return "ro";
                case "Russian":
                    return "ru";
                case "SerboCroatian":
                    return "sr";
                case "Slovak":
                    return "sk";
                case "Slovenian":
                    return "sl";
                case "Spanish":
                    return "es";
                case "Swedish":
                    return "sv";
                case "Thai":
                    return "th";
                case "Turkish":
                    return "tr";
                case "Ukrainian":
                    return "uk";
                case "Vietnamese":
                    return "vi";
                default:
                    return null;
            }
        }
    }
}
