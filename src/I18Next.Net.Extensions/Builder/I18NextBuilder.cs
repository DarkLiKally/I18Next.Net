using System;
using System.Collections.Generic;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.Extensions.Builder
{
    public class I18NextBuilder
    {
        private readonly List<Action<I18NextNet>> _configureCallbacks = new List<Action<I18NextNet>>();

        private readonly I18NextOptions _options = new I18NextOptions();

        public IServiceCollection Services { get; }

        public I18NextBuilder(IServiceCollection services)
        {
            Services = services;
        }
        
        public I18NextNet Build()
        {
            _options.Backend = _options.Backend ?? new JsonFileBackend();
            _options.Logger = _options.Logger ?? new TraceLogger();
            _options.LanguageDetector = _options.LanguageDetector ?? new DefaultLanguageDetector(_options.DefaultLanguage);
            _options.PluralResolver = _options.PluralResolver ?? new DefaultPluralResolver();
            _options.Interpolator = _options.Interpolator ?? new DefaultInterpolator();
            _options.Translator = _options.Translator ?? new DefaultTranslator(_options.Backend, _options.Logger, _options.PluralResolver, _options.Interpolator);

            var i18Next = new I18NextNet(_options.Backend, _options.Translator)
            {
                Language = _options.DefaultLanguage,
                DefaultNamespace = _options.DefaultNamespace,
                Logger = _options.Logger,
                LanguageDetector = _options.LanguageDetector
            };

            foreach (var postProcessor in _options.PostProcessors)
                i18Next.PostProcessors.Add(postProcessor);

            foreach (var callback in _configureCallbacks)
                callback?.Invoke(i18Next);

            return i18Next;
        }

        public I18NextBuilder ConfigureOptions(Action<I18NextOptions> configure)
        {
            configure(_options);

            return this;
        }

        public I18NextBuilder Configure(Action<I18NextNet> configure)
        {
            _configureCallbacks.Add(configure);

            return this;
        }

        public I18NextBuilder UseBackend(ITranslationBackend backend)
        {
            _options.Backend = backend;

            return this;
        }

        public I18NextBuilder UseBackend<T>()
            where T : ITranslationBackend
        {
            _options.Backend = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseBackend<T>(Action<T> configure)
            where T : ITranslationBackend
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.Backend = instance;

            return this;
        }

        public I18NextBuilder UseDefaultLanguage(string language)
        {
            _options.DefaultLanguage = language;

            return this;
        }

        public I18NextBuilder UseDefaultNamespace(string @namespace)
        {
            _options.DefaultNamespace = @namespace;

            return this;
        }

        public I18NextBuilder UseInterpolator(IInterpolator interpolator)
        {
            _options.Interpolator = interpolator;

            return this;
        }

        public I18NextBuilder UseInterpolator<T>()
            where T : IInterpolator
        {
            _options.Interpolator = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseInterpolator<T>(Action<T> configure)
            where T : IInterpolator
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.Interpolator = instance;

            return this;
        }

        public I18NextBuilder UseLanguageDetector<T>()
            where T : ILanguageDetector
        {
            _options.LanguageDetector = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseLanguageDetector<T>(Action<T> configure)
            where T : ILanguageDetector
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.LanguageDetector = instance;

            return this;
        }

        public I18NextBuilder UseLanguageDetector(ILanguageDetector languageDetector)
        {
            _options.LanguageDetector = languageDetector;

            return this;
        }

        public I18NextBuilder UseLanguageDetector(ILogger logger)
        {
            _options.Logger = logger;

            return this;
        }

        public I18NextBuilder UseLogger<T>()
            where T : ILogger
        {
            _options.Logger = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseLogger(ILogger logger)
        {
            _options.Logger = logger;

            return this;
        }

        public I18NextBuilder UseLogger<T>(Action<T> configure)
            where T : ILogger
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.Logger = instance;

            return this;
        }

        public I18NextBuilder UsePluralResolver(IPluralResolver backend)
        {
            _options.PluralResolver = backend;

            return this;
        }

        public I18NextBuilder UsePluralResolver<T>()
            where T : IPluralResolver
        {
            _options.PluralResolver = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UsePluralResolver<T>(Action<T> configure)
            where T : IPluralResolver
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.PluralResolver = instance;

            return this;
        }

        public I18NextBuilder UsePostprocessor(IPostProcessor postProcessor)
        {
            if (!_options.PostProcessors.Contains(postProcessor))
                _options.PostProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UsePostprocessor<T>()
            where T : IPostProcessor
        {
            var postProcessor = Activator.CreateInstance<T>();

            _options.PostProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UsePostprocessor<T>(Action<T> configure)
            where T : IPostProcessor
        {
            var postProcessor = Activator.CreateInstance<T>();

            configure?.Invoke(postProcessor);

            _options.PostProcessors.Add(postProcessor);

            return this;
        }

        public I18NextBuilder UseTranslator<T>()
            where T : ITranslator
        {
            _options.Translator = Activator.CreateInstance<T>();

            return this;
        }

        public I18NextBuilder UseTranslator(ITranslator logger)
        {
            _options.Translator = logger;

            return this;
        }

        public I18NextBuilder UseTranslator<T>(Action<T> configure)
            where T : ITranslator
        {
            var instance = Activator.CreateInstance<T>();

            configure?.Invoke(instance);

            _options.Translator = instance;

            return this;
        }
    }
}
