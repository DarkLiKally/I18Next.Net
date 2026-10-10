using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     A backend which is able to store the translations of a namespace, e.g. a cache or a database. A chained backend
///     stores namespaces provided by later backends in earlier writable backends when enabled.
/// </summary>
public interface IWritableTranslationBackend : ITranslationBackend
{
    /// <summary>
    ///     Stores the translations of a namespace and replaces the translations stored before.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="tree">The translations, <see cref="ITranslationTree.GetAllValues" /> provides them with dotted keys.</param>
    Task SaveNamespaceAsync(string language, string @namespace, ITranslationTree tree);
}
