using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
#if !NET6_0
using System.Text.Json.Serialization.Metadata;
#endif
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

namespace I18Next.Net;

/// <summary>
///     The default <see cref="II18Next" /> implementation. Resolves translations through an <see cref="ITranslator" />
///     (usually the <see cref="DefaultTranslator" />) which loads the namespaces from an <see cref="ITranslationBackend" />.
/// </summary>
/// <remarks>
///     The instance starts with the language <c>en-US</c>, the default namespace <c>translation</c> and no fallback
///     languages. Applications using dependency injection usually configure it with the builder of the
///     I18Next.Net.Extensions package instead of creating it directly.
/// </remarks>
/// <example>
///     <code>
///     var backend = new JsonFileBackend("locales");
///     var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de" };
///     i18Next.SetFallbackLanguages("en");
///
///     var greeting = i18Next.T("greeting", new { name = "Ada" });
///     </code>
/// </example>
public class I18NextNet : II18Next
{
    private static readonly HashSet<string> RightToLeftLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "shu", "sqr", "ssh", "xaa", "yhd", "yud", "aao", "abh", "abv", "acm", "acq", "acw", "acx", "acy", "adf", "ads", "aeb", "aec", "afb",
        "ajp", "apc", "apd", "arb", "arq", "ars", "ary", "arz", "auz", "avl", "ayh", "ayl", "ayn", "ayp", "bbz", "pga", "he", "iw", "ps", "pbt",
        "pbu", "pst", "prp", "prd", "ug", "ur", "ydd", "yds", "yih", "ji", "yi", "hbo", "men", "xmn", "fa", "jpr", "peo", "pes", "prs", "dv", "sam",
        "ckb"
    };
    private static readonly JsonSerializerOptions ObjectSerializerOptions = CreateObjectSerializerOptions();

    private readonly TranslationOptions _options;
    private JsonSerializerOptions _modelSerializerOptions;

    /// <summary>
    ///     Creates an instance translating with the given translator and backend.
    /// </summary>
    /// <param name="backend">The backend the translations are loaded from, exposed as <see cref="Backend" />.</param>
    /// <param name="translator">
    ///     The translator resolving the translations. It should load its translations from the same
    ///     <paramref name="backend" />.
    /// </param>
    /// <param name="languageDetector">
    ///     The language detector used by <see cref="UseDetectedLanguage" /> and
    ///     <see cref="DetectLanguageOnEachTranslation" />. Defaults to a <see cref="DefaultLanguageDetector" /> always
    ///     detecting <c>en-US</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="backend" /> or <paramref name="translator" /> is null.
    /// </exception>
    public I18NextNet(ITranslationBackend backend, ITranslator translator, ILanguageDetector languageDetector = null)
    {
        _options = CreateTranslationOptions();

        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        Translator = translator ?? throw new ArgumentNullException(nameof(translator));

        Language = "en-US";
        Logger = new TraceLogger();
        LanguageDetector = languageDetector ?? new DefaultLanguageDetector("en-US");
    }

    /// <summary>
    ///     The languages searched in order when a key is missing in the requested language, like the i18next
    ///     <c>fallbackLng</c> option. Empty by default. Overridden per language by <see cref="LanguageFallbacks" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null.</exception>
    public string[] FallbackLanguages
    {
        get => _options.FallbackLanguages;
        set => _options.FallbackLanguages = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     The namespaces searched in order when a key is missing in the requested namespace, like the i18next
    ///     <c>fallbackNS</c> option. They are searched in the requested language before the fallback languages are tried.
    ///     <c>null</c> by default.
    /// </summary>
    public string[] FallbackNamespaces
    {
        get => _options.FallbackNamespaces;
        set => _options.FallbackNamespaces = value;
    }

    /// <summary>
    ///     Fallback languages per language, like the object form of the i18next <c>fallbackLng</c> option. An entry for the
    ///     requested language, or else for its language part (<c>de</c> for <c>de-CH</c>), replaces
    ///     <see cref="FallbackLanguages" /> for that language. <c>null</c> by default.
    /// </summary>
    /// <seealso cref="SetLanguageFallbacks" />
    public IDictionary<string, string[]> LanguageFallbacks
    {
        get => _options.LanguageFallbacks;
        set => _options.LanguageFallbacks = value;
    }

    /// <summary>
    ///     The logger associated with this instance. Defaults to a <see cref="TraceLogger" />. The translator and the other
    ///     plugins receive their own logger when they are created.
    /// </summary>
    public ILogger Logger { get; set; }

    /// <summary>
    ///     The options used by <see cref="T{TModel}(string, object)" /> to map translations to models other than string
    ///     arrays and dictionaries. Set options with a <see cref="System.Text.Json.Serialization.JsonSerializerContext" /> as
    ///     type info resolver for trimmed and Native AOT applications.
    /// </summary>
    public JsonSerializerOptions ModelSerializerOptions
    {
        get => _modelSerializerOptions;
        set => _modelSerializerOptions = value == null
            ? null
            : new JsonSerializerOptions(value) { NumberHandling = value.NumberHandling | JsonNumberHandling.AllowReadingFromString };
    }

    /// <inheritdoc />
    public ITranslationBackend Backend { get; }

    /// <summary>
    ///     The namespace used for keys without a namespace prefix, like the i18next <c>defaultNS</c> option. Defaults to
    ///     <c>translation</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null, empty or whitespace.</exception>
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

    /// <inheritdoc />
    /// <remarks>
    ///     When the detector returns null, empty or whitespace, <see cref="Language" /> is used. Disabled by default.
    /// </remarks>
    public bool DetectLanguageOnEachTranslation { get; set; }

    /// <summary>
    ///     The language used for translations without an explicit language. Defaults to <c>en-US</c>. Setting a different
    ///     language raises <see cref="LanguageChanged" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null, empty or whitespace.</exception>
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

    /// <inheritdoc />
    public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

    /// <summary>
    ///     The language detector used by <see cref="UseDetectedLanguage" /> and <see cref="DetectLanguageOnEachTranslation" />.
    /// </summary>
    public ILanguageDetector LanguageDetector { get; set; }

    /// <inheritdoc />
    public string T(string key, object args = null)
    {
        return Translate(GetCurrentLanguage(), key, args, _options);
    }

    /// <inheritdoc />
    public string T(string language, string key, object args = null)
    {
        return Translate(language, key, args, _options);
    }

    /// <inheritdoc />
    public string T(string language, string defaultNamespace, string key, object args = null)
    {
        return Translate(language, key, args, CreateTranslationOptions(defaultNamespace));
    }

    /// <inheritdoc />
    public Task<string> Ta(string key, object args = null)
    {
        return Ta(GetCurrentLanguage(), key, args);
    }

    /// <inheritdoc />
    public Task<string> Ta(string language, string key, object args = null)
    {
        return Ta(language, key, args, _options);
    }

    /// <inheritdoc />
    public Task<string> Ta(string language, string defaultNamespace, string key, object args = null)
    {
        var options = CreateTranslationOptions(defaultNamespace);

        return Ta(language, key, args, options);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="keys" /> is null or empty.</exception>
    public string T(string[] keys, object args = null)
    {
        return Ta(keys, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="keys" /> is null or empty.</exception>
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

    /// <inheritdoc />
    public IDictionary<string, object> TObject(string key, object args = null)
    {
        return TaObject(key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IDictionary<string, object> TObject(string language, string key, object args = null)
    {
        return TaObject(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> TaObject(string key, object args = null)
    {
        return TaObject(GetCurrentLanguage(), key, args);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> TaObject(string language, string key, object args = null)
    {
        return Translator.TranslateObjectAsync(language, key, args.ToDictionary(), _options);
    }

    /// <inheritdoc />
    /// <remarks>The values are mapped like <see cref="Ta{TModel}(string, string, object)" /> describes.</remarks>
    public TModel T<TModel>(string key, object args = null)
    {
        return Ta<TModel>(key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    /// <remarks>The values are mapped like <see cref="Ta{TModel}(string, string, object)" /> describes.</remarks>
    public TModel T<TModel>(string language, string key, object args = null)
    {
        return Ta<TModel>(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    /// <remarks>The values are mapped like <see cref="Ta{TModel}(string, string, object)" /> describes.</remarks>
    public Task<TModel> Ta<TModel>(string key, object args = null)
    {
        return Ta<TModel>(GetCurrentLanguage(), key, args);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     A group whose keys are <c>0</c>, <c>1</c>, ... is treated as an array. Arrays of strings are mapped to
    ///     <c>string[]</c> or <see cref="List{T}" /> of string, groups of strings to dictionaries of strings, and
    ///     <see cref="object" /> or <see cref="IDictionary{TKey,TValue}" /> of object receive the translated values as they
    ///     are. All other types are deserialized from the translated values with <see cref="ModelSerializerOptions" /> or,
    ///     if not set, case-insensitive reflection based options which also read numbers from strings.
    /// </remarks>
    public async Task<TModel> Ta<TModel>(string language, string key, object args = null)
    {
        var values = await TaObject(language, key, args).ConfigureAwait(false);

        if (values == null)
            return default;

        object root = values;

        if (values.Count > 0 && Enumerable.Range(0, values.Count).All(i => values.ContainsKey(i.ToString())))
            root = Enumerable.Range(0, values.Count).Select(i => values[i.ToString()]).ToArray();

        if (TryMapToModel<TModel>(root, out var model))
            return model;

#if NET6_0
        return ToJsonNode(root).Deserialize<TModel>(ModelSerializerOptions ?? ObjectSerializerOptions);
#else
        var typeInfo = (JsonTypeInfo<TModel>)(ModelSerializerOptions ?? ObjectSerializerOptions).GetTypeInfo(typeof(TModel));

        return ToJsonNode(root).Deserialize(typeInfo);
#endif
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The reflection based resolver is only used when reflection based serialization is enabled.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The reflection based resolver is only used when reflection based serialization is enabled.")]
    private static JsonSerializerOptions CreateObjectSerializerOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString };

#if !NET6_0
        if (JsonSerializer.IsReflectionEnabledByDefault)
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
#endif

        return options;
    }

    private static bool TryMapToModel<TModel>(object root, out TModel model)
    {
        object result = null;

        if (typeof(TModel) == typeof(object) || root is IDictionary<string, object> && typeof(TModel).IsAssignableFrom(typeof(Dictionary<string, object>)))
            result = root;
        else if (root is object[] array && array.All(v => v is string))
            result = MapStrings(array.Cast<string>().ToArray(), typeof(TModel));
        else if (root is IDictionary<string, object> dictionary && dictionary.Values.All(v => v is string) &&
                 typeof(TModel).IsAssignableFrom(typeof(Dictionary<string, string>)))
            result = dictionary.ToDictionary(e => e.Key, e => (string)e.Value);

        model = result is TModel typed ? typed : default;

        return result is TModel;
    }

    private static object MapStrings(string[] values, Type type)
    {
        if (type.IsAssignableFrom(typeof(string[])))
            return values;

        return type.IsAssignableFrom(typeof(List<string>)) ? values.ToList() : null;
    }

    private static JsonNode ToJsonNode(object value)
    {
        switch (value)
        {
            case IDictionary<string, object> dictionary:
                var jsonObject = new JsonObject();

                foreach (var entry in dictionary)
                    jsonObject[entry.Key] = ToJsonNode(entry.Value);

                return jsonObject;
            case object[] array:
                var jsonArray = new JsonArray();

                foreach (var item in array)
                    jsonArray.Add(ToJsonNode(item));

                return jsonArray;
            case null:
                return null;
            default:
                return JsonValue.Create(value.ToString());
        }
    }

    /// <inheritdoc />
    public bool Exists(string key, object args = null)
    {
        return ExistsAsync(GetCurrentLanguage(), key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string language, string key, object args = null)
    {
        return Translator.ExistsAsync(language, key, args.ToDictionary(), _options);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Only the language part of the language is checked against the known right-to-left languages, e.g. <c>ar</c> for
    ///     <c>ar-EG</c>. Languages with the <c>Arab</c> script subtag, e.g. <c>ks-Arab</c>, are right-to-left as well.
    /// </remarks>
    public string Dir(string language = null)
    {
        language ??= GetCurrentLanguage();

        if (string.IsNullOrEmpty(language))
            return "ltr";

        var separatorIndex = language.IndexOfAny(['-', '_']);
        var languagePart = separatorIndex > -1 ? language.Substring(0, separatorIndex) : language;

        return RightToLeftLanguages.Contains(languagePart) || language.IndexOf("-Arab", StringComparison.OrdinalIgnoreCase) > -1 ? "rtl" : "ltr";
    }

    /// <inheritdoc />
    public ITranslator Translator { get; }

    /// <inheritdoc />
    public void UseDetectedLanguage()
    {
        Language = LanguageDetector.GetLanguage();
    }

    /// <summary>
    ///     Sets the <see cref="FallbackLanguages" /> searched when a key is missing in the requested language.
    /// </summary>
    /// <param name="languages">The fallback languages in order of preference.</param>
    /// <exception cref="ArgumentNullException"><paramref name="languages" /> is null.</exception>
    public void SetFallbackLanguages(params string[] languages)
    {
        FallbackLanguages = languages;
    }

    /// <summary>
    ///     Sets the <see cref="FallbackNamespaces" /> searched when a key is missing in the requested namespace.
    /// </summary>
    /// <param name="namespaces">The fallback namespaces in order of preference.</param>
    public void SetFallbackNamespaces(params string[] namespaces)
    {
        FallbackNamespaces = namespaces;
    }

    /// <summary>
    ///     Sets the fallback languages of a single language in <see cref="LanguageFallbacks" />, creating the dictionary with
    ///     a case-insensitive comparer if it does not exist yet.
    /// </summary>
    /// <param name="language">
    ///     The language, e.g. <c>de-CH</c>, or a language part like <c>de</c> to match all its regions.
    /// </param>
    /// <param name="fallbackLanguages">
    ///     The fallback languages in order of preference, used instead of <see cref="FallbackLanguages" /> for
    ///     <paramref name="language" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="language" /> is null, empty or whitespace or <paramref name="fallbackLanguages" /> is null.
    /// </exception>
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
