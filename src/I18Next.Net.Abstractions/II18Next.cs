using System;
using System.Threading.Tasks;

namespace I18Next.Net
{
    public interface II18Next
    {
        string Language { get; }
        
        string DefaultNamespace { get; set; }

        string T(string key, object args = null);

        string T(string language, string key, object args = null);

        string T(string language, string defaultNamespace, string key, object args = null);

        Task<string> Ta(string key, object args = null);

        Task<string> Ta(string language, string key, object args = null);

        Task<string> Ta(string language, string defaultNamespace, string key, object args = null);

        void UseDetectedLanguage();
        
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;
    }
}
