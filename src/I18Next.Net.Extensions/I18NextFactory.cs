using System.Linq;

using I18Next.Net.Backends;
using I18Next.Net.Extensions.Configuration;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Microsoft.Extensions.Options;

namespace I18Next.Net.Extensions;

/// <summary>
///     Creates <see cref="I18NextNet" /> instances with the registered plugins and the <see cref="I18NextOptions" />.
/// </summary>
/// <param name="backend">The backend of the instances.</param>
/// <param name="translator">The translator of the instances.</param>
/// <param name="languageDetector">The language detector of the instances.</param>
/// <param name="logger">The logger of the instances.</param>
/// <param name="options">The options applied to the instances.</param>
public class I18NextFactory(ITranslationBackend backend, ITranslator translator, ILanguageDetector languageDetector, ILogger logger,
    IOptions<I18NextOptions> options) : II18NextFactory
{
    private readonly ITranslationBackend _backend = backend;
    private readonly ILanguageDetector _languageDetector = languageDetector;
    private readonly ILogger _logger = logger;
    private readonly IOptions<I18NextOptions> _options = options;
    private readonly ITranslator _translator = translator;

    /// <summary>
    ///     Creates a new <see cref="I18NextNet" /> instance with the registered plugins, using
    ///     <see cref="I18NextOptions.DefaultLanguage" /> as language and the namespace and fallback options.
    /// </summary>
    /// <returns>The new instance.</returns>
    public II18Next CreateInstance()
    {
        var instance = new I18NextNet(_backend, _translator, _languageDetector)
        {
            Language = _options.Value.DefaultLanguage,
            DefaultNamespace = _options.Value.DefaultNamespace,
            Logger = _logger,
            DetectLanguageOnEachTranslation = _options.Value.DetectLanguageOnEachTranslation,
            ModelSerializerOptions = _options.Value.ModelSerializerOptions
        };
        instance.SetFallbackLanguages([.. _options.Value.FallbackLanguages]);
        instance.SetFallbackNamespaces([.. _options.Value.FallbackNamespaces]);

        foreach (var languageFallback in _options.Value.LanguageFallbacks)
            instance.SetLanguageFallbacks(languageFallback.Key, languageFallback.Value);

        return instance;
    }
}
