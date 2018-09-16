using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins
{
    public interface ITranslator
    {
        Task<string> TranslateAsync(string language, string defaultNamespace, string key, IDictionary<string, object> args);
        
        List<IPostProcessor> PostProcessors { get; }
    }
}
