using System;
using System.IO;
using System.Threading;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using UnityEngine;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     Holds the I18Next instance used by the localized components and raises its events on the main thread.
    /// </summary>
    public static class I18NextUnity
    {
        /// <summary>
        ///     The folder below a <c>Resources</c> folder the default instance loads the translations from, e.g.
        ///     <c>Resources/locales/en/translation.json</c>.
        /// </summary>
        public const string DefaultResourcesPath = "locales";

        private static readonly object SyncRoot = new object();

        private static INotifyingTranslationBackend _backend;
        private static II18Next _instance;
        private static int _mainThreadId;
        private static SynchronizationContext _synchronizationContext;

        /// <summary>
        ///     The instance translating the components. A default instance created by <see cref="CreateDefault" /> is used
        ///     until another instance is set. Setting another instance translates all components again.
        /// </summary>
        public static II18Next Instance
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                        Attach(CreateDefault());

                    return _instance;
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    if (ReferenceEquals(_instance, value))
                        return;

                    Attach(value);
                }

                OnTranslationsChanged(value, new TranslationsChangedEventArgs(null, null));
            }
        }

        /// <summary>
        ///     The language of <see cref="Instance" />. Setting it translates all components again.
        /// </summary>
        public static string Language
        {
            get => Instance.Language;
            set => Instance.Language = value;
        }

        /// <summary>
        ///     Raised on the main thread after the language of <see cref="Instance" /> has changed.
        /// </summary>
        public static event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        /// <summary>
        ///     Raised on the main thread after the translations of <see cref="Instance" /> have changed or another instance
        ///     has been set.
        /// </summary>
        public static event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

        /// <summary>
        ///     Translates the given key into the language of <see cref="Instance" />.
        /// </summary>
        /// <param name="key">Key to be translated, optionally prefixed with a namespace like <c>common:save</c>.</param>
        /// <param name="args">Additional arguments used to translate the key.</param>
        /// <returns>Translation value.</returns>
        public static string T(string key, object args = null)
        {
            return Instance.T(key, args);
        }

        /// <summary>
        ///     Creates an instance loading the i18next JSON files from <c>Resources</c>, using the i18next v4 plural suffixes,
        ///     the language of the operating system and logging to the Unity console.
        /// </summary>
        /// <param name="fallbackLanguage">The language used for missing translations and unknown system languages.</param>
        /// <param name="resourcesPath">The folder below a <c>Resources</c> folder containing the translations.</param>
        /// <returns>The I18Next instance.</returns>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="fallbackLanguage" /> is <c>null</c>, empty or whitespace.
        /// </exception>
        public static I18NextNet CreateDefault(string fallbackLanguage = "en", string resourcesPath = DefaultResourcesPath)
        {
            if (string.IsNullOrWhiteSpace(fallbackLanguage))
                throw new ArgumentNullException(nameof(fallbackLanguage));

            var logger = new UnityLogger();
            var backend = CreateResourcesBackend(resourcesPath);
            var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
            var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));
            var i18Next = new I18NextNet(backend, translator, new SystemLanguageDetector(fallbackLanguage))
            {
                FallbackLanguages = new[] { fallbackLanguage },
                Logger = logger
            };

            i18Next.UseDetectedLanguage();

            return i18Next;
        }

        /// <summary>
        ///     Creates a backend loading the JSON content of a namespace from the text asset
        ///     <c>Resources/{path}/{lng}/{ns}.json</c>. Works on all platforms, translations have to be loaded on the main
        ///     thread. When a regional language like <c>de-AT</c> has no file, the file of the language part <c>de</c> is
        ///     loaded.
        /// </summary>
        /// <param name="path">The folder below a <c>Resources</c> folder containing the translations.</param>
        /// <returns>The backend.</returns>
        public static FuncBackend CreateResourcesBackend(string path = DefaultResourcesPath)
        {
            var prefix = string.IsNullOrEmpty(path) ? string.Empty : path.Trim('/') + "/";

            return FuncBackend.FromJson((language, ns) => LoadTextAsset($"{prefix}{language}/{ns}"));
        }

        /// <summary>
        ///     Creates a backend reading the JSON files <c>StreamingAssets/{path}/{lng}/{ns}.json</c>, which can be replaced
        ///     after the build. Requires synchronous file access, so it does not work on Android and WebGL. When a regional
        ///     language like <c>de-AT</c> has no file, the file of the language part <c>de</c> is loaded.
        /// </summary>
        /// <param name="path">The folder below the <c>StreamingAssets</c> folder containing the translations.</param>
        /// <returns>The backend.</returns>
        public static FuncBackend CreateStreamingAssetsBackend(string path = DefaultResourcesPath)
        {
            var basePath = string.IsNullOrEmpty(path) ? Application.streamingAssetsPath : Path.Combine(Application.streamingAssetsPath, path);

            return FuncBackend.FromJson((language, ns) =>
            {
                var file = Path.Combine(basePath, language, ns + ".json");

                return File.Exists(file) ? File.ReadAllText(file) : null;
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                Attach(null);

                _synchronizationContext = SynchronizationContext.Current;
                _mainThreadId = Environment.CurrentManagedThreadId;
            }

            LanguageChanged = null;
            TranslationsChanged = null;
        }

        private static void Attach(II18Next instance)
        {
            if (_instance != null)
                _instance.LanguageChanged -= OnLanguageChanged;

            if (_backend != null)
                _backend.TranslationsChanged -= OnTranslationsChanged;

            _instance = instance;
            _backend = instance?.Backend as INotifyingTranslationBackend;

            if (_instance != null)
                _instance.LanguageChanged += OnLanguageChanged;

            if (_backend != null)
                _backend.TranslationsChanged += OnTranslationsChanged;
        }

        private static string LoadTextAsset(string path)
        {
            var asset = Resources.Load<TextAsset>(path);

            if (asset == null)
                return null;

            var text = asset.text;
            Resources.UnloadAsset(asset);

            return text;
        }

        private static void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            Dispatch(() => LanguageChanged?.Invoke(sender, e));
        }

        private static void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
        {
            Dispatch(() => TranslationsChanged?.Invoke(sender, e));
        }

        private static void Dispatch(Action action)
        {
            var synchronizationContext = _synchronizationContext;

            if (synchronizationContext == null || Environment.CurrentManagedThreadId == _mainThreadId)
                action();
            else
                synchronizationContext.Post(_ => action(), null);
        }
    }
}
