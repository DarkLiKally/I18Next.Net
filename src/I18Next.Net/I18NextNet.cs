using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net
{
    public class I18NextNet : II18Next
    {
        private string _language;

        public I18NextNet(ITranslationBackend backend, ITranslator translator)
        {
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            Translator = translator ?? throw new ArgumentNullException(nameof(translator));

            Language = "en-US";
            DefaultNamespace = "translation";
            Logger = new TraceLogger();
            LanguageDetector = new DefaultLanguageDetector(Language);
        }

        public ITranslationBackend Backend { get; }

        public string DefaultNamespace { get; set; }

        public bool DetectLanguageOnEachTranslation { get; set; }

        public string Language
        {
            get => _language;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(value);

                if (value == _language)
                    return;

                var oldLang = _language;
                _language = value;

                OnLanguageChanged(new LanguageChangedEventArgs(oldLang, _language));
            }
        }

        public ILanguageDetector LanguageDetector { get; set; }

        public ILogger Logger { get; set; }

        public List<IPostProcessor> PostProcessors => Translator.PostProcessors;

        public ITranslator Translator { get; }

        public string T(string key, object args = null)
        {
            return Ta(_language, key, args).Result;
        }

        public string T(string language, string key, object args = null)
        {
            return Ta(language, key, args).Result;
        }

        public string T(string language, string defaultNamespace, string key, object args = null)
        {
            return Ta(language, defaultNamespace, key, args).Result;
        }

        public Task<string> Ta(string key, object args = null)
        {
            return Ta(_language, key, args);
        }

        public Task<string> Ta(string language, string key, object args = null)
        {
            return Ta(language, DefaultNamespace, key, args);
        }

        public async Task<string> Ta(string language, string defaultNamespace, string key, object args = null)
        {
            if (DetectLanguageOnEachTranslation)
                UseDetectedLanguage();

            var argsDict = args.ToDictionary();

            return await Translator.TranslateAsync(language, defaultNamespace, key, argsDict);
        }

        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        public void UseDetectedLanguage()
        {
            Language = LanguageDetector.GetLanguage();
        }

        private void OnLanguageChanged(LanguageChangedEventArgs e)
        {
            LanguageChanged?.Invoke(this, e);
        }
    }
}
