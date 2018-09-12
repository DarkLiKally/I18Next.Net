using System.Threading;

namespace I18Next.Net.Plugins
{
    public class ThreadLanguageDetector : ILanguageDetector
    {
        public string FallbackLanguage { get; set; }

        public ThreadLanguageDetector()
        {
            FallbackLanguage = "en-US";
        }
        
        public ThreadLanguageDetector(string fallbackLanguage)
        {
            FallbackLanguage = fallbackLanguage;
        }
        
        public string GetLanguage() => Thread.CurrentThread.CurrentCulture.IetfLanguageTag;
    }
}
