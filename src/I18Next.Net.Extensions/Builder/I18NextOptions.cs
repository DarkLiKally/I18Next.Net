using System.Collections.Generic;

namespace I18Next.Net.Extensions.Builder
{
    public class I18NextOptions
    {
        public string DefaultLanguage { get; set; } = "en-US";

        public string DefaultNamespace { get; set; } = "translation";
        public IList<string> FallbackLanguages { get; set; }

        public bool DetectLanguageOnEachTranslation { get; set; }
    }
}
