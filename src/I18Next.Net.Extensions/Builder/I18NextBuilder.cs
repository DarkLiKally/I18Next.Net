using System;
using System.Collections.Generic;
using System.Linq;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace I18Next.Net.Extensions.Builder
{
    public class I18NextBuilder
    {
        public I18NextBuilder(IServiceCollection services)
        {
            Services = services;
            Services.AddOptions<I18NextOptions>();
        }

        public IServiceCollection Services { get; }

        public I18NextBuilder AddBackend(ITranslationBackend backend)
        {
            Services.AddSingleton(backend);

            return this;
        }

        public I18NextBuilder AddBackend<T>()
            where T : class, ITranslationBackend
        {
            Services.AddSingleton<ITranslationBackend, T>();

            return this;
        }

        public I18NextBuilder AddBackend<T>(Func<IServiceProvider, T> factory)
            where T : class, ITranslationBackend
        {
            Services.AddSingleton<ITranslationBackend, T>(factory);

            return this;
        }

        public I18NextBuilder AddFormatter(IFormatter formatter)
        {
            Services.AddSingleton(formatter);

            return this;
        }

        public I18NextBuilder AddFormatter<T>()
            where T : class, IFormatter
        {
            Services.AddSingleton<IFormatter, T>();

            return this;
        }

        public I18NextBuilder AddFormatter<T>(Func<IServiceProvider, T> factory)
            where T : class, IFormatter
        {
            Services.AddSingleton<IFormatter, T>(factory);

            return this;
        }

        public I18NextBuilder AddInterpolator(IInterpolator interpolator)
        {
            Services.AddSingleton(interpolator);

            return this;
        }

        public I18NextBuilder AddInterpolator<T>()
            where T : class, IInterpolator
        {
            Services.AddSingleton<IInterpolator, T>();

            return this;
        }

        public I18NextBuilder AddInterpolator<T>(Func<IServiceProvider, T> factory)
            where T : class, IInterpolator
        {
            Services.AddSingleton<IInterpolator, T>(factory);

            return this;
        }

        public I18NextBuilder AddLanguageDetector(ILanguageDetector languageDetector)
        {
            Services.AddSingleton(languageDetector);

            return this;
        }

        public I18NextBuilder AddLanguageDetector<T>()
            where T : class, ILanguageDetector
        {
            Services.AddSingleton<ILanguageDetector, T>();

            return this;
        }

        public I18NextBuilder AddLanguageDetector<T>(Func<IServiceProvider, T> factory)
            where T : class, ILanguageDetector
        {
            Services.AddSingleton<ILanguageDetector, T>(factory);

            return this;
        }

        public I18NextBuilder AddLogger(ILogger backend)
        {
            Services.AddSingleton(backend);

            return this;
        }

        public I18NextBuilder AddLogger<T>()
            where T : class, ILogger
        {
            Services.AddSingleton<ILogger, T>();

            return this;
        }

        public I18NextBuilder AddLogger<T>(Func<IServiceProvider, T> factory)
            where T : class, ILogger
        {
            Services.AddSingleton<ILogger, T>(factory);

            return this;
        }

        public I18NextBuilder AddPluralResolver(IPluralResolver pluralResolver)
        {
            Services.AddSingleton(pluralResolver);

            return this;
        }

        public I18NextBuilder AddPluralResolver<T>()
            where T : class, IPluralResolver
        {
            Services.AddSingleton<IPluralResolver, T>();

            return this;
        }

        public I18NextBuilder AddPluralResolver<T>(Func<IServiceProvider, T> factory)
            where T : class, IPluralResolver
        {
            Services.AddSingleton<IPluralResolver, T>(factory);

            return this;
        }

        public I18NextBuilder AddPostProcessor(IPostProcessor postProcessor)
        {
            Services.AddSingleton(postProcessor);

            return this;
        }

        public I18NextBuilder AddPostProcessor<T>()
            where T : class, IPostProcessor
        {
            Services.AddSingleton<IPostProcessor, T>();

            return this;
        }

        public I18NextBuilder AddPostProcessor<T>(Func<IServiceProvider, T> factory)
            where T : class, IPostProcessor
        {
            Services.AddSingleton<IPostProcessor, T>(factory);

            return this;
        }

        public I18NextBuilder AddTranslator<T>()
            where T : class, ITranslator
        {
            Services.AddSingleton<ITranslator, T>();

            return this;
        }

        public I18NextBuilder AddTranslator(ITranslator translator)
        {
            Services.AddSingleton(translator);

            return this;
        }

        public I18NextBuilder AddTranslator<T>(Func<IServiceProvider, T> factory)
            where T : class, ITranslator
        {
            Services.AddSingleton<ITranslator, T>(factory);

            return this;
        }

        public void Build()
        {
            AddSingletonIfNotPresent<ILogger, DefaultExtensionsLogger>();
            AddSingletonIfNotPresent<IPluralResolver, DefaultPluralResolver>();
            AddSingletonIfNotPresent<ILanguageDetector, DefaultLanguageDetector>(DefaultLanguageDetectorFactory);
            AddSingletonIfNotPresent<ITranslationBackend, JsonFileBackend>();
            AddSingletonIfNotPresent<ITranslator, DefaultTranslator>(DefaultTranslatorFactory);
            AddSingletonIfNotPresent<IInterpolator, DefaultInterpolator>(DefaultInterpolatorFactory);

            Services.AddSingleton<II18NextFactory, I18NextFactory>();
            Services.AddSingleton(c => c.GetRequiredService<II18NextFactory>().CreateInstance());

            Services.AddSingleton<IStringLocalizerFactory, I18NextStringLocalizerFactory>();
            Services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }

        public I18NextBuilder Configure(Action<I18NextOptions> configure)
        {
            Services.Configure<I18NextOptions>(options => configure?.Invoke(options));

            return this;
        }

        public I18NextBuilder UseDefaultLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("Language cannot be null or empty.", nameof(language));

            Services.Configure<I18NextOptions>(options => options.DefaultLanguage = language);

            return this;
        }

        public I18NextBuilder UseDefaultNamespace(string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace))
                throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));

            Services.Configure<I18NextOptions>(options => options.DefaultNamespace = @namespace);

            return this;
        }

        private void AddSingletonIfNotPresent<TService, TImplementation>()
            where TImplementation : class, TService
            where TService : class
        {
            if (Services.All(s => s.ServiceType != typeof(TService)))
                Services.AddSingleton<TService, TImplementation>();
        }

        private void AddSingletonIfNotPresent<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory)
            where TImplementation : class, TService
            where TService : class
        {
            if (Services.All(s => s.ServiceType != typeof(TService)))
                Services.AddSingleton<TService, TImplementation>(factory);
        }

        private DefaultInterpolator DefaultInterpolatorFactory(IServiceProvider c)
        {
            var formatters = c.GetRequiredService<IEnumerable<IFormatter>>();

            var instance = new DefaultInterpolator();

            instance.Formatters.AddRange(formatters);

            return instance;
        }

        private DefaultLanguageDetector DefaultLanguageDetectorFactory(IServiceProvider c)
        {
            var options = c.GetRequiredService<IOptions<I18NextOptions>>();

            return new DefaultLanguageDetector(options.Value.DefaultLanguage);
        }

        private DefaultTranslator DefaultTranslatorFactory(IServiceProvider c)
        {
            var backend = c.GetRequiredService<ITranslationBackend>();
            var logger = c.GetRequiredService<ILogger>();
            var pluralResolver = c.GetRequiredService<IPluralResolver>();
            var interpolator = c.GetRequiredService<IInterpolator>();
            var postProcessors = c.GetRequiredService<IEnumerable<IPostProcessor>>();

            var instance = new DefaultTranslator(backend, logger, pluralResolver, interpolator);

            instance.PostProcessors.AddRange(postProcessors);

            return instance;
        }
    }
}
