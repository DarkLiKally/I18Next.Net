using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     A backend plugin which loads the translations of a namespace in a language, e.g. from files, resources, a database
///     or a server. The translator caches loaded namespaces, see <see cref="IExpiringTranslationBackend" /> and
///     <see cref="INotifyingTranslationBackend" /> to reload them.
/// </summary>
public interface ITranslationBackend
{
    /// <summary>
    ///     Loads the translations of a namespace in a language.
    /// </summary>
    /// <param name="language">
    ///     The language as requested, e.g. <c>de-AT</c>. Backends usually fall back to the language part (e.g. <c>de</c>)
    ///     when there are no translations for the regional language.
    /// </param>
    /// <param name="namespace">The namespace, e.g. <c>translation</c>.</param>
    /// <returns>
    ///     A task resolving to the translations or to <c>null</c> if the namespace does not exist for the language, so
    ///     the translator continues with the fallback namespaces and languages. Throw only for actual failures, e.g. an
    ///     unreachable server or an invalid file.
    /// </returns>
    Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace);
}
