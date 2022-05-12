using System;
using System.Collections.Generic;
using System.Linq;
using I18Next.Net.Backends;
using I18Next.Net.Extensions.Configuration;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace I18Next.Net.Extensions.Builder;

/// <summary>
///     Allows registering I18Next into an IServiceCollection using simple fluent configuration methods.
/// </summary>
public class I18NextBuilder
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="services">Underlying service collection.</param>
    public I18NextBuilder(IServiceCollection services)
    {
        Services = services;
        Services.AddOptions<I18NextOptions>();
    }

    /// <summary>
    ///     Provides direct access to the underlying IServiceCollection used to register the services and plugins.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Registers the provided instance of a translation backend plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one backend at a time. By default the last registered
    ///         backend will be used.
    ///         You can use a CompositeBackend to combine multiple backend implementations into one.
    ///     </para>
    /// </remarks>
    /// <param name="backend">The translation backend instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddBackend(ITranslationBackend backend)
    {
        Services.AddSingleton(backend);

        return this;
    }

    /// <summary>
    ///     Registers a new translation backend plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one backend at a time. By default the last registered
    ///         backend will be used.
    ///         You can use a CompositeBackend to combine multiple backend implementations into one.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">Type of the translation backend.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddBackend<T>()
        where T : class, ITranslationBackend
    {
        Services.AddSingleton<ITranslationBackend, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new translation backend plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one backend at a time. By default the last registered
    ///         backend will be used.
    ///         You can use a CompositeBackend to combine multiple backend implementations into one.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Translation backend factory function.</param>
    /// <typeparam name="T">Type of the translation backend.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddBackend<T>(Func<IServiceProvider, T> factory)
        where T : class, ITranslationBackend
    {
        Services.AddSingleton<ITranslationBackend, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a formatter plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used interpolator implementation whether formatters will be used. The
    ///         default interpolator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple formatters.</para>
    /// </remarks>
    /// <param name="formatter">The formatter plugin instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddFormatter(IFormatter formatter)
    {
        Services.AddSingleton(formatter);

        return this;
    }

    /// <summary>
    ///     Registers a new formatter plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used interpolator implementation whether formatters will be used. The
    ///         default interpolator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple formatters.</para>
    /// </remarks>
    /// <para>It is possible to use multiple formatters.</para>
    /// <typeparam name="T">The formatter plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddFormatter<T>()
        where T : class, IFormatter
    {
        Services.AddSingleton<IFormatter, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new formatter plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used interpolator implementation whether formatters will be used. The
    ///         default interpolator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple formatters.</para>
    /// </remarks>
    /// <param name="factory">Formatter plugin factory function.</param>
    /// <typeparam name="T">Type of the formatter plugin.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddFormatter<T>(Func<IServiceProvider, T> factory)
        where T : class, IFormatter
    {
        Services.AddSingleton<IFormatter, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a translation interpolator plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one interpolator at a time. By default the last registered
    ///         interpolator will be used.
    ///         It's not supported to combine multiple interpolator plugins.
    ///     </para>
    /// </remarks>
    /// <param name="interpolator">The translation interpolator instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddInterpolator(IInterpolator interpolator)
    {
        Services.AddSingleton(interpolator);

        return this;
    }

    /// <summary>
    ///     Registers a new interpolator plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one interpolator at a time. By default the last registered
    ///         interpolator will be used.
    ///         It's not supported to combine multiple interpolator plugins.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The interpolator plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddInterpolator<T>()
        where T : class, IInterpolator
    {
        Services.AddSingleton<IInterpolator, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new interpolator plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one interpolator at a time. By default the last registered
    ///         interpolator will be used.
    ///         It's not supported to combine multiple interpolator plugins.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Interpolator plugin factory function.</param>
    /// <typeparam name="T">The interpolator plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddInterpolator<T>(Func<IServiceProvider, T> factory)
        where T : class, IInterpolator
    {
        Services.AddSingleton<IInterpolator, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a language detector plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one language detector at a time. By default the last
    ///         registered language detector will be used.
    ///     </para>
    /// </remarks>
    /// <param name="languageDetector">The language detector instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLanguageDetector(ILanguageDetector languageDetector)
    {
        Services.AddSingleton(languageDetector);

        return this;
    }

    /// <summary>
    ///     Registers a new language detector plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one language detector at a time. By default the last
    ///         registered language detector will be used.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The language detector plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLanguageDetector<T>()
        where T : class, ILanguageDetector
    {
        Services.AddSingleton<ILanguageDetector, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new language detector plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one language detector at a time. By default the last
    ///         registered language detector will be used.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Language detector plugin factory function.</param>
    /// <typeparam name="T">The language detector plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLanguageDetector<T>(Func<IServiceProvider, T> factory)
        where T : class, ILanguageDetector
    {
        Services.AddSingleton<ILanguageDetector, T>(factory);

        return this;
    }


    /// <summary>
    ///     Registers the provided instance of a logger plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one logger at a time. By default the last registered
    ///         logger will be used.
    ///     </para>
    /// </remarks>
    /// <param name="backend">The logger instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLogger(ILogger backend)
    {
        Services.AddSingleton(backend);

        return this;
    }

    /// <summary>
    ///     Registers a new logger plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one logger at a time. By default the last registered
    ///         logger will be used.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The logger plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLogger<T>()
        where T : class, ILogger
    {
        Services.AddSingleton<ILogger, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new logger plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one logger at a time. By default the last registered
    ///         logger will be used.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Logger plugin factory function.</param>
    /// <typeparam name="T">The logger plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddLogger<T>(Func<IServiceProvider, T> factory)
        where T : class, ILogger
    {
        Services.AddSingleton<ILogger, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a missing key handler plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether missing key handlers will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple missing key handlers.</para>
    /// </remarks>
    /// <param name="missingKeyHandler">The missing key handler instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddMissingKeyHandler(IMissingKeyHandler missingKeyHandler)
    {
        Services.AddSingleton(missingKeyHandler);

        return this;
    }

    /// <summary>
    ///     Registers a new missing key handler plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether missing key handlers will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple missing key handlers.</para>
    /// </remarks>
    /// <typeparam name="T">The missing key handler plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddMissingKeyHandler<T>()
        where T : class, IMissingKeyHandler
    {
        Services.AddSingleton<IMissingKeyHandler, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new missing key handler plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether missing key handlers will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple missing key handlers.</para>
    /// </remarks>
    /// <param name="factory">Missing key handler plugin factory function.</param>
    /// <typeparam name="T">The missing key handler plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddMissingKeyHandler<T>(Func<IServiceProvider, T> factory)
        where T : class, IMissingKeyHandler
    {
        Services.AddSingleton<IMissingKeyHandler, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a plural resolver plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether a registered plural resolver will be used.
    ///         The default translator will use it.
    ///     </para>
    ///     <para>
    ///         Note: The configuring default I18Next instance can only use one plural resolver at a time. By default the last
    ///         registered plural resolver will be used.
    ///     </para>
    /// </remarks>
    /// <param name="pluralResolver">The plural resolver instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPluralResolver(IPluralResolver pluralResolver)
    {
        Services.AddSingleton(pluralResolver);

        return this;
    }

    /// <summary>
    ///     Registers a new plural resolver plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether a registered plural resolver will be used.
    ///         The default translator will use it.
    ///     </para>
    ///     <para>
    ///         Note: The configuring default I18Next instance can only use one logger at a time. By default the last
    ///         registered logger will be used.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The plural resolver plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPluralResolver<T>()
        where T : class, IPluralResolver
    {
        Services.AddSingleton<IPluralResolver, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new plural resolver plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether a registered plural resolver will be used.
    ///         The default translator will use it.
    ///     </para>
    ///     <para>
    ///         Note: The configuring default I18Next instance can only use one plural resolver at a time. By default the last
    ///         registered plural resolver will be used.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Plural resolver plugin factory function.</param>
    /// <typeparam name="T">The plural resolver plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPluralResolver<T>(Func<IServiceProvider, T> factory)
        where T : class, IPluralResolver
    {
        Services.AddSingleton<IPluralResolver, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a post processor plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether post processors will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple post processors.</para>
    /// </remarks>
    /// <param name="postProcessor">The post processor instance.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPostProcessor(IPostProcessor postProcessor)
    {
        Services.AddSingleton(postProcessor);

        return this;
    }

    /// <summary>
    ///     Registers a new post processor plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether post processors will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple post processors.</para>
    /// </remarks>
    /// <typeparam name="T">The post processor plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPostProcessor<T>()
        where T : class, IPostProcessor
    {
        Services.AddSingleton<IPostProcessor, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new post processor plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: It's dependent on the used translator implementation whether post processors will be used. The
    ///         default translator will use it.
    ///     </para>
    ///     <para>It is possible to use multiple post processors.</para>
    /// </remarks>
    /// <param name="factory">Post processor plugin factory function.</param>
    /// <typeparam name="T">The post processor plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddPostProcessor<T>(Func<IServiceProvider, T> factory)
        where T : class, IPostProcessor
    {
        Services.AddSingleton<IPostProcessor, T>(factory);

        return this;
    }

    /// <summary>
    ///     Registers the provided instance of a translator plugin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one translator at a time. By default the last registered
    ///         translator will be used.
    ///         It's not supported to combine multiple translator plugins.
    ///     </para>
    /// </remarks>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddTranslator<T>()
        where T : class, ITranslator
    {
        Services.AddSingleton<ITranslator, T>();

        return this;
    }

    /// <summary>
    ///     Registers a new translator plugin of the given type.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one translator at a time. By default the last registered
    ///         translator will be used.
    ///         It's not supported to combine multiple translator plugins.
    ///     </para>
    /// </remarks>
    /// <param name="translator">The translator plugin type.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddTranslator(ITranslator translator)
    {
        Services.AddSingleton(translator);

        return this;
    }

    /// <summary>
    ///     Registers a new translator plugin using a factory function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note: The configuring I18Next instance can only use one translator at a time. By default the last registered
    ///         translator will be used.
    ///         It's not supported to combine multiple translator plugins.
    ///     </para>
    /// </remarks>
    /// <param name="factory">Translator plugin factory function.</param>
    /// <typeparam name="T">The translator plugin type.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder AddTranslator<T>(Func<IServiceProvider, T> factory)
        where T : class, ITranslator
    {
        Services.AddSingleton<ITranslator, T>(factory);

        return this;
    }

    /// <summary>
    ///     Build the final service configurations and applies them to the underlying IServiceCollection.
    /// </summary>
    public void Build()
    {
        AddSingletonIfNotPresent(DefaultLoggerFactory);
        AddSingletonIfNotPresent<IPluralResolver, DefaultPluralResolver>();
        AddSingletonIfNotPresent<ILanguageDetector>(DefaultLanguageDetectorFactory);
        AddSingletonIfNotPresent<ITranslationBackend, JsonFileBackend>();
        AddSingletonIfNotPresent<ITranslator>(DefaultTranslatorFactory);
        AddSingletonIfNotPresent<IInterpolator>(DefaultInterpolatorFactory);

        Services.AddSingleton<II18NextFactory, I18NextFactory>();
        Services.AddSingleton(c => c.GetRequiredService<II18NextFactory>().CreateInstance());

        Services.AddSingleton<IStringLocalizerFactory, I18NextStringLocalizerFactory>();
        Services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        Services.TryAddTransient(typeof(IStringLocalizer), c => c.GetRequiredService<IStringLocalizerFactory>().Create(null));
    }

    /// <summary>
    ///     Allows configuration of some global I18Next options.
    /// </summary>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public I18NextBuilder Configure(Action<I18NextOptions> configure)
    {
        Services.Configure<I18NextOptions>(options => configure?.Invoke(options));

        return this;
    }

    /// <summary>
    ///     Sets the global default language used by I18Next to translate keys.
    /// </summary>
    /// <remarks>
    ///     <para>Allowed format examples: de-DE, en-US, en, de, en-GB</para>
    /// </remarks>
    /// <param name="language">The default language identifier.</param>
    /// <returns>The current I18Next builder instance.</returns>
    /// <exception cref="ArgumentException">If the provided default language value is null or empty.</exception>
    public I18NextBuilder UseDefaultLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            throw new ArgumentException("Language cannot be null or empty.", nameof(language));

        Services.Configure<I18NextOptions>(options => options.DefaultLanguage = language);

        return this;
    }

    /// <summary>
    ///     Sets the global default namespace used by I18Next to resolve translations.
    /// </summary>
    /// <param name="namespace">The default namespace.</param>
    /// <returns>The current I18Next builder instance.</returns>
    /// <exception cref="ArgumentException">If the provided default namespace value is null or empty.</exception>
    public I18NextBuilder UseDefaultNamespace(string @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
            throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));

        Services.Configure<I18NextOptions>(options => options.DefaultNamespace = @namespace);

        return this;
    }

    /// <summary>
    ///     Sets one or more globally used fallback languages used by I18Next to resolve translations if the default or
    ///     requested language does
    ///     not provide a value for a requested key. The fallback languages will be checked in given order until a value is
    ///     found.
    /// </summary>
    /// <remarks>
    ///     <para>Allowed format examples: de-DE, en-US, en, de, en-GB</para>
    /// </remarks>
    /// <param name="languages">One or more fallback language identifiers.</param>
    /// <returns>The current I18Next builder instance.</returns>
    /// <exception cref="ArgumentException">
    ///     If no fallback language was provided or any of the provided values is null or
    ///     empty.
    /// </exception>
    public I18NextBuilder UseFallbackLanguage(params string[] languages)
    {
        if (languages.Length == 0)
            throw new ArgumentException("Please supply at least one fallback language", nameof(languages));
        if (languages.Any(string.IsNullOrEmpty))
            throw new ArgumentException("None of fallback languages can be null or empty.", nameof(languages));

        Services.Configure<I18NextOptions>(options => options.FallbackLanguages = languages);

        return this;
    }

    private void AddSingletonIfNotPresent<TService, TImplementation>()
        where TImplementation : class, TService
        where TService : class
    {
        if (Services.All(s => s.ServiceType != typeof(TService)))
            Services.AddSingleton<TService, TImplementation>();
    }

    private void AddSingletonIfNotPresent<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        if (Services.All(s => s.ServiceType != typeof(TService)))
            Services.AddSingleton(factory);
    }

    private static DefaultInterpolator DefaultInterpolatorFactory(IServiceProvider c)
    {
        var formatters = c.GetRequiredService<IEnumerable<IFormatter>>();

        var logger = c.GetRequiredService<ILogger>();
        var instance = new DefaultInterpolator(logger);

        instance.Formatters.AddRange(formatters);

        return instance;
    }

    private static DefaultLanguageDetector DefaultLanguageDetectorFactory(IServiceProvider c)
    {
        var options = c.GetRequiredService<IOptions<I18NextOptions>>();

        return new DefaultLanguageDetector(options.Value.DefaultLanguage);
    }

    private static ILogger DefaultLoggerFactory(IServiceProvider c)
    {
        var msLogger = c.GetService<Microsoft.Extensions.Logging.ILogger>();

        if (msLogger != null)
            return new DefaultExtensionsLogger(msLogger);

        return new TraceLogger();
    }

    private static DefaultTranslator DefaultTranslatorFactory(IServiceProvider c)
    {
        var backend = c.GetRequiredService<ITranslationBackend>();
        var logger = c.GetRequiredService<ILogger>();
        var pluralResolver = c.GetRequiredService<IPluralResolver>();
        var interpolator = c.GetRequiredService<IInterpolator>();
        var postProcessors = c.GetService<IEnumerable<IPostProcessor>>();
        var missingKeyHandlers = c.GetService<IEnumerable<IMissingKeyHandler>>();

        var instance = new DefaultTranslator(backend, logger, pluralResolver, interpolator);

        if (postProcessors != null)
            instance.PostProcessors.AddRange(postProcessors);

        if (missingKeyHandlers != null)
            instance.MissingKeyHandlers.AddRange(missingKeyHandlers);

        return instance;
    }
}
