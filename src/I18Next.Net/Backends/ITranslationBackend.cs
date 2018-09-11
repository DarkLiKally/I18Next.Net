using System.Threading.Tasks;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends
{
    public interface ITranslationBackend
    {
        Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace);
    }
}
