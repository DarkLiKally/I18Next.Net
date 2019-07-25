using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins
{
    public interface ITranslator
    {
        event EventHandler<MissingKeyEventArgs> MissingKey;

        List<IPostProcessor> PostProcessors { get; }

        Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options);
    }
}
