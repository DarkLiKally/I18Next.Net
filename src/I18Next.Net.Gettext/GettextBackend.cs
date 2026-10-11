using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using NGettext;

namespace I18Next.Net.Gettext;

/// <summary>
///     Backend for retrieving translations from compiled Gettext <c>{basePath}/{language}/{namespace}.mo</c> files,
///     falling back to the folder of the language part (e.g. <c>de</c> for <c>de-AT</c>). The message ids are used as
///     keys.
/// </summary>
/// <remarks>
///     <para>
///         The plural forms of a message are loaded as separate keys suffixed with a plural suffix, see
///         <see cref="UseSimplePluralSuffix" /> and <see cref="PluralSeparator" />. The plural rules of the mo file
///         header are ignored, the I18Next plural resolver chooses the plural form instead.
///     </para>
/// </remarks>
/// <param name="basePath">The folder containing the language folders.</param>
public class GettextBackend(string basePath) : ITranslationBackend
{
    private readonly string _basePath = basePath;

    /// <summary>
    ///     Creates a backend loading the files from the <c>locales</c> folder.
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
    ///     Whether to use simple plural suffixes: no suffix for the first form, <c>plural</c> for the second form and the
    ///     form number for further forms (e.g. <c>key</c>, <c>key_plural</c>, <c>key_3</c>). If set to false all
    ///     translations are suffixed with their form number starting at 1 (e.g. <c>key_1</c>, <c>key_2</c>).
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

        return Task.FromResult((ITranslationTree)translationTree);
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
