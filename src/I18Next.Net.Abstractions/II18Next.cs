using System;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net
{
    public interface II18Next
    {
        ITranslationBackend Backend { get; }
        
        ITranslator Translator { get; }

        string DefaultNamespace { get; set; }

        bool DetectLanguageOnEachTranslation { get; set; }

        string Language { get; set; }

        event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        string T(string key, object args = null);

        string T(string language, string key, object args = null);

        string T(string language, string defaultNamespace, string key, object args = null);

        Task<string> Ta(string key, object args = null);

        Task<string> Ta(string language, string key, object args = null);

        Task<string> Ta(string language, string defaultNamespace, string key, object args = null);

        void UseDetectedLanguage();
    }
}
