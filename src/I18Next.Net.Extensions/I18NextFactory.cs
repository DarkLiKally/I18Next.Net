using System.Linq;
using I18Next.Net.Backends;
using I18Next.Net.Extensions.Builder;
using I18Next.Net.Plugins;
using Microsoft.Extensions.Options;

namespace I18Next.Net.Extensions
{
    public class I18NextFactory : II18NextFactory
    {
        private readonly ITranslationBackend _backend;
        private readonly ILanguageDetector _languageDetector;
        private readonly ILogger _logger;
        private readonly IOptions<I18NextOptions> _options;
        private readonly ITranslator _translator;

        public I18NextFactory(ITranslationBackend backend, ITranslator translator, ILanguageDetector languageDetector, ILogger logger,
            IOptions<I18NextOptions> options)
        {
            _backend = backend;
            _translator = translator;
            _languageDetector = languageDetector;
            _logger = logger;
            _options = options;
        }

        public II18Next CreateInstance()
        {
            var instance = new I18NextNet(_backend, _translator, _languageDetector)
            {
                Language = _options.Value.DefaultLanguage,
                DefaultNamespace = _options.Value.DefaultNamespace,
                Logger = _logger,
                DetectLanguageOnEachTranslation = _options.Value.DetectLanguageOnEachTranslation,
            };
            instance.SetFallbackLanguage(_options.Value.FallbackLanguages.ToArray());

            return instance;
        }
    }
}
