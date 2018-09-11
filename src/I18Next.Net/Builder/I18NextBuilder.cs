using System;
using System.Collections.Generic;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net.Builder
{
    public class I18NextBuilder
    {
        private readonly List<Action<I18Next>> _configureCallbacks = new List<Action<I18Next>>();
        private readonly List<IPostProcessor> _postProcessors = new List<IPostProcessor>();

        public ITranslationBackend Backend { get; private set; }

        public string DefaultLanguage { get; private set; } = "en-US";

        public string DefaultNamespace { get; private set; } = "translation";

        public IInterpolator Interpolator { get; private set; }

        public ILanguageDetector LanguageDetector { get; private set; }

        public ILogger Logger { get; private set; }

        public IPluralResolver PluralResolver { get; private set; }

        public ITranslator Translator { get; private set; }

        public I18Next Build()
        {
            Backend = Backend ?? new JsonFileBackend();
            Logger = Logger ?? new TraceLogger();
            LanguageDetector = LanguageDetector ?? new DefaultLanguageDetector(DefaultLanguage);
            PluralResolver = PluralResolver ?? new DefaultPluralResolver();
            Interpolator = Interpolator ?? new DefaultInterpolator();
            Translator = Translator ?? new DefaultTranslator(Backend, Logger, PluralResolver, Interpolator);

            var i18Next = new I18Next(Backend, Translator)
            {
                Language = DefaultLanguage,
                DefaultNamespace = DefaultNamespace,
                Logger = Logger,
                LanguageDetector = LanguageDetector
            };

            foreach (var postProcessor in _postProcessors)
                i18Next.PostProcessors.Add(postProcessor);

            foreach (var callback in _configureCallbacks)
                callback?.Invoke(i18Next);

            return i18Next;
        }

        public I18NextBuilder Configure(Action<I18Next> configure)
        {
            _configureCallbacks.Add(configure);

            return this;
        }

        public I18NextBuilder UseBackend(ITranslationBackend backend)
        {
            Backend = backend;

            return this;
        }

        public I18NextBuilder UseBackend<T>()
            where T : ITranslationBackend
        {
            Backend = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseBackend<T>(Action<T> configure)
            where T : ITranslationBackend
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            Backend = instance;

            return this;
        }

        public I18NextBuilder UseDefaultLanguage(string language)
        {
            DefaultLanguage = language;

            return this;
        }

        public I18NextBuilder UseDefaultNamespace(string @namespace)
        {
            DefaultNamespace = @namespace;

            return this;
        }

        public I18NextBuilder UseInterpolator(IInterpolator interpolator)
        {
            Interpolator = interpolator;

            return this;
        }

        public I18NextBuilder UseInterpolator<T>()
            where T : IInterpolator
        {
            Interpolator = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseInterpolator<T>(Action<T> configure)
            where T : IInterpolator
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            Interpolator = instance;

            return this;
        }

        public I18NextBuilder UseLanguageDetector<T>()
            where T : ILanguageDetector
        {
            LanguageDetector = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseLanguageDetector<T>(Action<T> configure)
            where T : ILanguageDetector
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            LanguageDetector = instance;

            return this;
        }

        public I18NextBuilder UseLanguageDetector(ILanguageDetector languageDetector)
        {
            LanguageDetector = languageDetector;

            return this;
        }

        public I18NextBuilder UseLanguageDetector(ILogger logger)
        {
            Logger = logger;

            return this;
        }

        public I18NextBuilder UseLogger<T>()
            where T : ILogger
        {
            Logger = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseLogger(ILogger logger)
        {
            Logger = logger;

            return this;
        }

        public I18NextBuilder UseLogger<T>(Action<T> configure)
            where T : ILogger
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            Logger = instance;

            return this;
        }

        public I18NextBuilder UsePluralResolver(IPluralResolver backend)
        {
            PluralResolver = backend;

            return this;
        }

        public I18NextBuilder UsePluralResolver<T>()
            where T : IPluralResolver
        {
            PluralResolver = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UsePluralResolver<T>(Action<T> configure)
            where T : IPluralResolver
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            PluralResolver = instance;

            return this;
        }

        public I18NextBuilder UsePostprocessor(IPostProcessor postProcessor)
        {
            if (!_postProcessors.Contains(postProcessor))
                _postProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UsePostprocessor<T>()
            where T : IPostProcessor
        {
            var postProcessor = Activator.CreateInstance<T>();

            _postProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UsePostprocessor<T>(Action<T> configure)
            where T : IPostProcessor
        {
            var postProcessor = Activator.CreateInstance<T>();

            configure?.Invoke(postProcessor);

            _postProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UseTranslator<T>()
            where T : ITranslator
        {
            Translator = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseTranslator(ITranslator logger)
        {
            Translator = logger;

            return this;
        }

        public I18NextBuilder UseTranslator<T>(Action<T> configure)
            where T : ITranslator
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            Translator = instance;

            return this;
        }
    }
}
