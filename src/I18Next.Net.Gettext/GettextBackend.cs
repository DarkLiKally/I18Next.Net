using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;
using NGettext;

namespace I18Next.Net.Gettext
{
    /// <summary>
    ///     Backend for retrieving translations from a compiled Gettext mo-file.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Translated string arrays are loaded as plurals and get suffixed with the corresponding plural suffix for
    ///         further internal processing.
    ///         Use the provided pluralization options to customize the outcome of plural suffixes.
    ///         The provided pluralization rules from the mo file header is ignored. We're simply using the I18Next plural
    ///         resolver later.
    ///     </para>
    /// </remarks>
    public class GettextBackend : ITranslationBackend
    {
        private readonly string _basePath;

        /// <summary>
        ///     Constructor providing a base path override.
        /// </summary>
        /// <param name="basePath"></param>
        public GettextBackend(string basePath)
        {
            _basePath = basePath;
        }

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public GettextBackend()
            : this("locales")
        {
        }

        /// <summary>
        ///     Separator used between the actual key and the plural suffix.
        /// </summary>
        public string PluralSeparator { get; set; } = "_";

        /// <summary>
        ///     Whether to use simple plural suffixes ('plural' for plural and nothing for singular).
        ///     If set to false all translations would be suffixed with the corresponding plural number.
        /// </summary>
        public bool UseSimplePluralSuffix { get; set; } = true;

        /// <inheritdoc />
        public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
        {
            var translationTree = new DictionaryTranslationTree(@namespace);

            var path = FindFile(language, @namespace);

            if (path == null)
                return null;

            using (Stream moFileStream = File.OpenRead(path))
            {
                var catalog = new Catalog(moFileStream, CultureInfo.GetCultureInfo(language));

                foreach (var translation in catalog.Translations)
                {
                    for (var i = 0; i < translation.Value.Length; i++)
                    {
                        var key = translation.Key;
                        var value = translation.Value[i];
                        var pluralNumber = i + 1;

                        if (UseSimplePluralSuffix)
                        {
                            if (i == 1)
                                key += PluralSeparator + "plural";
                            else if (i > 1)
                                key += PluralSeparator + pluralNumber;
                        }
                        else
                        {
                            key += PluralSeparator + pluralNumber;
                        }

                        translationTree.AddValue(key, value);
                    }
                }
            }

            return Task.FromResult((ITranslationTree) translationTree);
        }

        private string FindFile(string language, string @namespace)
        {
            var path = Path.Combine(_basePath, language, @namespace + ".mo");

            if (File.Exists(path))
                return path;

            path = Path.Combine(_basePath, BackendUtilities.GetLanguagePart(language), @namespace + ".mo");

            return !File.Exists(path) ? null : path;
        }
    }
}
