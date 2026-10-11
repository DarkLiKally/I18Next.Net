using System;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace I18Next.Net.Blazor;

/// <summary>
///     Holds the language of the current user and translates with the shared <see cref="II18Next" /> instance. Registered
///     as a scoped service, so every circuit on the server has its own language.
/// </summary>
public sealed class BlazorI18Next : IBlazorI18Next, IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     The path of the JavaScript module which reads the stored language from the browser and stores the chosen
    ///     language, the <c>lang</c> and the <c>dir</c> attribute of the document.
    /// </summary>
    public const string ModulePath = "./_content/I18Next.Net.Blazor/i18next.js";

    private const string PreloadKey = "__i18next_blazor_preload__";

    private readonly II18Next _i18Next;
    private readonly IJSRuntime _jsRuntime;
    private readonly INotifyingTranslationBackend _notifyingBackend;
    private readonly I18NextBlazorOptions _options;

    private int _languageVersion;
    private IJSObjectReference _module;

    /// <summary>
    ///     Creates the translator of a user. The initial language is detected by the language detector of the
    ///     <paramref name="i18Next" /> instance and matched against the supported languages. Subscribes to
    ///     <see cref="INotifyingTranslationBackend.TranslationsChanged" /> if the backend supports it.
    /// </summary>
    /// <param name="i18Next">The shared I18Next instance used to translate.</param>
    /// <param name="options">The supported languages, the preloaded namespaces and how the language is stored.</param>
    /// <param name="jsRuntime">The JavaScript runtime used to read and store the language in the browser.</param>
    /// <exception cref="ArgumentNullException">If an argument is <c>null</c>.</exception>
    public BlazorI18Next(II18Next i18Next, IOptions<I18NextBlazorOptions> options, IJSRuntime jsRuntime)
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

        Language = ResolveLanguage(_i18Next.LanguageDetector?.GetLanguage());

        _notifyingBackend = _i18Next.Backend as INotifyingTranslationBackend;

        if (_notifyingBackend != null)
            _notifyingBackend.TranslationsChanged += OnTranslationsChanged;
    }

    /// <inheritdoc />
    public II18Next Instance => _i18Next;

    /// <inheritdoc />
    public string Language { get; private set; }

    /// <inheritdoc />
    public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

    /// <inheritdoc />
    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    /// <inheritdoc />
    /// <remarks>
    ///     The stored language is ignored while JavaScript is not available, e.g. during prerendering. When
    ///     <see cref="ChangeLanguageAsync" /> is called before the initialization completes, the newer language wins.
    /// </remarks>
    public async Task InitializeAsync()
    {
        var version = ++_languageVersion;
        var storedLanguage = await GetStoredLanguageAsync();
        var language = storedLanguage == null ? Language : ResolveLanguage(storedLanguage);

        await LoadAsync(language, []);

        if (version == _languageVersion)
            SetLanguage(language);
    }

    /// <inheritdoc />
    public async Task ChangeLanguageAsync(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentNullException(nameof(language));

        var version = ++_languageVersion;
        language = ResolveLanguage(language);

        await LoadAsync(language, []);

        if (version != _languageVersion)
            return;

        SetLanguage(language);

        await StoreLanguageAsync(language);
    }

    /// <inheritdoc />
    public Task LoadNamespacesAsync(params string[] namespaces)
    {
        return LoadAsync(Language, namespaces);
    }

    /// <inheritdoc />
    public string T(string key, object args = null)
    {
        return _i18Next.T(Language, key, args);
    }

    /// <inheritdoc />
    public TModel T<TModel>(string key, object args = null)
    {
        return _i18Next.T<TModel>(Language, key, args);
    }

    /// <inheritdoc />
    public bool Exists(string key, object args = null)
    {
        return _i18Next.ExistsAsync(Language, key, args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public FixedT GetFixedT(string @namespace = null, string keyPrefix = null)
    {
        return new FixedT(_i18Next, Language, @namespace, keyPrefix, GetNamespaceSeparator());
    }

    /// <inheritdoc />
    public string Dir()
    {
        return _i18Next.Dir(Language);
    }

    /// <summary>
    ///     Unsubscribes from the changed translations of the backend.
    /// </summary>
    public void Dispose()
    {
        if (_notifyingBackend != null)
            _notifyingBackend.TranslationsChanged -= OnTranslationsChanged;
    }

    /// <summary>
    ///     Unsubscribes from the changed translations of the backend and releases the imported JavaScript module. A
    ///     disconnected circuit is ignored.
    /// </summary>
    /// <returns>A task which completes when the module is released.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();

        if (_module == null)
            return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }

        _module = null;
    }

    private Task LoadAsync(string language, string[] namespaces)
    {
        if (namespaces == null || namespaces.Length == 0)
            namespaces = _options.Namespaces is { Count: > 0 } ? [.. _options.Namespaces] : [_i18Next.DefaultNamespace];

        var separator = GetNamespaceSeparator();

        return Task.WhenAll(namespaces.Select(ns => _i18Next.ExistsAsync(language, ns + separator + PreloadKey)));
    }

    private void SetLanguage(string language)
    {
        if (language == Language)
            return;

        var oldLanguage = Language;
        Language = language;

        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, language));
    }

    private string ResolveLanguage(string language)
    {
        var supportedLanguages = _options.SupportedLanguages;

        if (supportedLanguages == null || supportedLanguages.Count == 0)
            return string.IsNullOrWhiteSpace(language) ? _i18Next.Language : language;

        return FindSupportedLanguage(language) ?? FindSupportedLanguage(_i18Next.Language) ?? supportedLanguages[0];
    }

    private string FindSupportedLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        var supportedLanguages = _options.SupportedLanguages;
        var languagePart = BackendUtilities.GetLanguagePart(language);

        return supportedLanguages.FirstOrDefault(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase))
               ?? supportedLanguages.FirstOrDefault(l => string.Equals(l, languagePart, StringComparison.OrdinalIgnoreCase))
               ?? supportedLanguages.FirstOrDefault(l => string.Equals(BackendUtilities.GetLanguagePart(l), languagePart, StringComparison.OrdinalIgnoreCase));
    }

    private string GetNamespaceSeparator()
    {
        return (_i18Next.Translator as DefaultTranslator)?.NamespaceSeparator ?? ":";
    }

    private async Task<string> GetStoredLanguageAsync()
    {
        if (_options.StorageKey == null && _options.CookieName == null)
            return null;

        try
        {
            var module = await GetModuleAsync();

            return await module.InvokeAsync<string>("getLanguage", _options.StorageKey, _options.CookieName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException)
        {
            return null;
        }
    }

    private async Task StoreLanguageAsync(string language)
    {
        try
        {
            var module = await GetModuleAsync();

            await module.InvokeVoidAsync("setLanguage", language, _i18Next.Dir(language), _options.StorageKey, _options.CookieName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException)
        {
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        return _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        TranslationsChanged?.Invoke(this, e);
    }
}
