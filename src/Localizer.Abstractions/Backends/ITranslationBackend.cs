using System.Threading.Tasks;
using Localizer.TranslationTrees;

namespace Localizer.Backends;

public interface ITranslationBackend
{
    Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace);
}
