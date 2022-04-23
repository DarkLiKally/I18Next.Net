using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins;

public interface ITranslator
{
    List<IPostProcessor> PostProcessors { get; }

    event EventHandler<MissingKeyEventArgs> MissingKey;

    Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options);
}