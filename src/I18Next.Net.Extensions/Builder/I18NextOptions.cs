using System.Collections.Generic;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net.Extensions.Builder
{
    public class I18NextOptions
    {

        public ITranslationBackend Backend { get; set; }

        public string DefaultLanguage { get; set; } = "en-US";

        public string DefaultNamespace { get; set; } = "translation";

        public IInterpolator Interpolator { get; set; }

        public ILanguageDetector LanguageDetector { get; set; }

        public ILogger Logger { get; set; }

        public IPluralResolver PluralResolver { get; set; }

        public ITranslator Translator { get; set; }
        
        public List<IPostProcessor> PostProcessors = new List<IPostProcessor>();
        
        public List<IFormatter> Formatters = new List<IFormatter>();
    }
}
