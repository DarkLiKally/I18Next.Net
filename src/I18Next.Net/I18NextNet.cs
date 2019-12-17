using System;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;

namespace I18Next.Net
{
    public class I18NextNet : II18Next
    {
        private string _defaultNamespace;
        private string[] _fallbackLanguages;
        private string _language;

        private readonly TranslationOptions _options;

        public I18NextNet(ITranslationBackend backend, ITranslator translator)
            : this(backend, translator, null)
        {
        }

        public I18NextNet(ITranslationBackend backend, ITranslator translator, ILanguageDetector languageDetector)
        {
            _options = CreateTranslationOptions();

            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            Translator = translator ?? throw new ArgumentNullException(nameof(translator));

            Language = "en-US";
            DefaultNamespace = "translation";
            Logger = new TraceLogger();
            LanguageDetector = languageDetector ?? new DefaultLanguageDetector("en-US");
        }

        public string[] FallbackLanguages
        {
            get => _fallbackLanguages;
            set
            {
                _fallbackLanguages = value ?? throw new ArgumentNullException(nameof(value));

                UpdateTranslationOptions();
            }
        }

        public ILogger Logger { get; set; }

        public ITranslationBackend Backend { get; }

        public string DefaultNamespace
        {
            get => _defaultNamespace;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));

                _defaultNamespace = value;

                UpdateTranslationOptions();
            }
        }

        public bool DetectLanguageOnEachTranslation { get; set; }

        public string Language
        {
            get => _language;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));

                if (value == _language)
                    return;

                var oldLang = _language;
                _language = value;

                OnLanguageChanged(new LanguageChangedEventArgs(oldLang, _language));
            }
        }

        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        public ILanguageDetector LanguageDetector { get; set; }

        public string T(string key, object args = null)
        {
            return Ta(_language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public string T(string language, string key, object args = null)
        {
            return Ta(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public string T(string language, string defaultNamespace, string key, object args = null)
        {
            return Ta(language, defaultNamespace, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task<string> Ta(string key, object args = null)
        {
            return Ta(_language, key, args);
        }

        public Task<string> Ta(string language, string key, object args = null)
        {
            return Ta(language, key, args, _options);
        }

        public Task<string> Ta(string language, string defaultNamespace, string key, object args = null)
        {
            var options = CreateTranslationOptions(defaultNamespace);

            return Ta(language, key, args, options);
        }

        public ITranslator Translator { get; }

        public void UseDetectedLanguage()
        {
            Language = LanguageDetector.GetLanguage();
        }

        public void SetFallbackLanguages(params string[] languages)
        {
            FallbackLanguages = languages;
        }

        private TranslationOptions CreateTranslationOptions(string defaultNamespace = null)
        {
            var options = new TranslationOptions
            {
                FallbackLanguages = _fallbackLanguages
            };

            if (defaultNamespace == null && _defaultNamespace != null)
                options.DefaultNamespace = _defaultNamespace;
            else if (defaultNamespace != null)
                options.DefaultNamespace = defaultNamespace;

            return options;
        }

        private void OnLanguageChanged(LanguageChangedEventArgs e)
        {
            LanguageChanged?.Invoke(this, e);
        }

        private async Task<string> Ta(string language, string key, object args, TranslationOptions options)
        {
            if (DetectLanguageOnEachTranslation)
                UseDetectedLanguage();

            var argsDict = args.ToDictionary();

            return await Translator.TranslateAsync(language, key, argsDict, options);
        }

        private void UpdateTranslationOptions()
        {
            _options.DefaultNamespace = DefaultNamespace;
            _options.FallbackLanguages = _fallbackLanguages;
        }
    }
}
