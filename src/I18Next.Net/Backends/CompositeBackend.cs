using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

public class CompositeBackend(params ITranslationBackend[] backends) : ITranslationBackend
{
    private readonly ITranslationBackend[] _backends = backends;

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        foreach (var backend in _backends)
        {
            var tree = await backend.LoadNamespaceAsync(language, @namespace).ConfigureAwait(false);

            if (tree != null)
                return tree;
        }

        return null;
    }
}
