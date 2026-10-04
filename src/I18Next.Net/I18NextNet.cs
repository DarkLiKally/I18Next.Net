using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

namespace I18Next.Net;

public class I18NextNet : II18Next
{
    private static readonly HashSet<string> RightToLeftLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "shu", "sqr", "ssh", "xaa", "yhd", "yud", "aao", "abh", "abv", "acm", "acq", "acw", "acx", "acy", "adf", "ads", "aeb", "aec", "afb",
        "ajp", "apc", "apd", "arb", "arq", "ars", "ary", "arz", "auz", "avl", "ayh", "ayl", "ayn", "ayp", "bbz", "pga", "he", "iw", "ps", "pbt",
        "pbu", "pst", "prp", "prd", "ug", "ur", "ydd", "yds", "yih", "ji", "yi", "hbo", "men", "xmn", "fa", "jpr", "peo", "pes", "prs", "dv", "sam",
        "ckb"
    };
    private static readonly JsonSerializerOptions ObjectSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly TranslationOptions _options;

    public I18NextNet(ITranslationBackend backend, ITranslator translator, ILanguageDetector languageDetector = null)
    {
        _options = CreateTranslationOptions();

        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        Translator = translator ?? throw new ArgumentNullException(nameof(translator));

        Language = "en-US";
        Logger = new TraceLogger();
        LanguageDetector = languageDetector ?? new DefaultLanguageDetector("en-US");
    }

    public string[] FallbackLanguages
    {
        get => _options.FallbackLanguages;
        set => _options.FallbackLanguages = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string[] FallbackNamespaces
    {
        get => _options.FallbackNamespaces;
        set => _options.FallbackNamespaces = value;
    }

    public IDictionary<string, string[]> LanguageFallbacks
    {
        get => _options.LanguageFallbacks;
        set => _options.LanguageFallbacks = value;
    }

    public ILogger Logger { get; set; }

    public ITranslationBackend Backend { get; }

    public string DefaultNamespace
    {
        get => _options.DefaultNamespace;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            _options.DefaultNamespace = value;
        }
    }

    public bool DetectLanguageOnEachTranslation { get; set; }

    public string Language
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value == field)
                return;

            var oldLang = field;
            field = value;

            OnLanguageChanged(new LanguageChangedEventArgs(oldLang, field));
        }
    }

    public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

    public ILanguageDetector LanguageDetector { get; set; }

    public string T(string key, object args = null)
    {
        return Translate(GetCurrentLanguage(), key, args, _options);
    }

    public string T(string language, string key, object args = null)
    {
        return Translate(language, key, args, _options);
    }

    public string T(string language, string defaultNamespace, string key, object args = null)
    {
        return Translate(language, key, args, CreateTranslationOptions(defaultNamespace));
    }

    public Task<string> Ta(string key, object args = null)
    {
        return Ta(GetCurrentLanguage(), key, args);
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

    public string T(string[] keys, object args = null)
    {
        return Ta(keys, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<string> Ta(string[] keys, object args = null)
    {
        if (keys == null || keys.Length == 0)
            throw new ArgumentNullException(nameof(keys));

        var language = GetCurrentLanguage();
        var argsDict = args.ToDictionary();

        for (var i = 0; i < keys.Length - 1; i++)
        {
            if (await Translator.ExistsAsync(language, keys[i], argsDict, _options).ConfigureAwait(false))
                return await Translator.TranslateAsync(language, keys[i], argsDict, _options).ConfigureAwait(false);
        }

        return await Translator.TranslateAsync(language, keys[keys.Length - 1], argsDict, _options).ConfigureAwait(false);
    }

    public IDictionary<string, object> TObject(string key, object args = null)
    {
        return TaObject(key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public IDictionary<string, object> TObject(string language, string key, object args = null)
    {
        return TaObject(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task<IDictionary<string, object>> TaObject(string key, object args = null)
    {
        return TaObject(GetCurrentLanguage(), key, args);
    }

    public Task<IDictionary<string, object>> TaObject(string language, string key, object args = null)
    {
        return Translator.TranslateObjectAsync(language, key, args.ToDictionary(), _options);
    }

    public TModel T<TModel>(string key, object args = null)
    {
        return Ta<TModel>(key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public TModel T<TModel>(string language, string key, object args = null)
    {
        return Ta<TModel>(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task<TModel> Ta<TModel>(string key, object args = null)
    {
        return Ta<TModel>(GetCurrentLanguage(), key, args);
    }

    public async Task<TModel> Ta<TModel>(string language, string key, object args = null)
    {
        var values = await TaObject(language, key, args).ConfigureAwait(false);

        if (values == null)
            return default;

        object root = values;

        if (values.Count > 0 && Enumerable.Range(0, values.Count).All(i => values.ContainsKey(i.ToString())))
            root = Enumerable.Range(0, values.Count).Select(i => values[i.ToString()]).ToArray();

        return JsonSerializer.Deserialize<TModel>(JsonSerializer.Serialize(root), ObjectSerializerOptions);
    }

    public bool Exists(string key, object args = null)
    {
        return ExistsAsync(GetCurrentLanguage(), key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task<bool> ExistsAsync(string language, string key, object args = null)
    {
        return Translator.ExistsAsync(language, key, args.ToDictionary(), _options);
    }

    public string Dir(string language = null)
    {
        language ??= GetCurrentLanguage();

        if (string.IsNullOrEmpty(language))
            return "ltr";

        var separatorIndex = language.IndexOfAny(['-', '_']);
        var languagePart = separatorIndex > -1 ? language.Substring(0, separatorIndex) : language;

        return RightToLeftLanguages.Contains(languagePart) || language.IndexOf("-Arab", StringComparison.OrdinalIgnoreCase) > -1 ? "rtl" : "ltr";
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

    public void SetFallbackNamespaces(params string[] namespaces)
    {
        FallbackNamespaces = namespaces;
    }

    public void SetLanguageFallbacks(string language, params string[] fallbackLanguages)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentNullException(nameof(language));

        LanguageFallbacks ??= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        LanguageFallbacks[language] = fallbackLanguages ?? throw new ArgumentNullException(nameof(fallbackLanguages));
    }

    private TranslationOptions CreateTranslationOptions(string defaultNamespace = null)
    {
        return _options != null
            ? new TranslationOptions
            {
                FallbackLanguages = _options.FallbackLanguages,
                FallbackNamespaces = _options.FallbackNamespaces,
                LanguageFallbacks = _options.LanguageFallbacks,
                DefaultNamespace = defaultNamespace ?? _options.DefaultNamespace
            }
            : new TranslationOptions
            {
                FallbackLanguages = [],
                DefaultNamespace = defaultNamespace ?? "translation"
            };
    }

    private string GetCurrentLanguage()
    {
        if (!DetectLanguageOnEachTranslation || LanguageDetector == null)
            return Language;

        var detectedLanguage = LanguageDetector.GetLanguage();

        return string.IsNullOrWhiteSpace(detectedLanguage) ? Language : detectedLanguage;
    }

    private void OnLanguageChanged(LanguageChangedEventArgs e)
    {
        LanguageChanged?.Invoke(this, e);
    }

    private string Translate(string language, string key, object args, TranslationOptions options)
    {
        if (Translator is not DefaultTranslator translator || translator.GetType() != typeof(DefaultTranslator))
            return Ta(language, key, args, options).ConfigureAwait(false).GetAwaiter().GetResult();

        var result = translator.TranslateValueAsync(language, key, args.ToDictionary(), options);

        return result.IsCompletedSuccessfully ? result.Result : result.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private Task<string> Ta(string language, string key, object args, TranslationOptions options)
    {
        var argsDict = args.ToDictionary();

        return Translator.TranslateAsync(language, key, argsDict, options);
    }
}
