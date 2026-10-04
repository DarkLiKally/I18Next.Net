using System;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Extensions.Builder;
using I18Next.Net.Formatters;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NUnit.Framework;

namespace I18Next.Net.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsFixture
{
    private static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        backend.AddTranslation("en", "common", "commonKey", "Common English text.");
        backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");
        backend.AddTranslation("de", "translation", "upper", "{{value, uppercase}}");
        backend.AddTranslation("de", "translation", "item_one", "ein Element");
        backend.AddTranslation("de", "translation", "item_other", "{{count}} Elemente");

        return backend;
    }

    [Test]
    public void AddI18NextLocalization_Defaults_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslationBackend>().Should().BeOfType<JsonFileBackend>();
        provider.GetRequiredService<ITranslator>().Should().BeOfType<DefaultTranslator>();
        provider.GetRequiredService<IInterpolator>().Should().BeOfType<DefaultInterpolator>();
        provider.GetRequiredService<IPluralResolver>().Should().BeOfType<DefaultPluralResolver>();
        provider.GetRequiredService<ILanguageDetector>().Should().BeOfType<DefaultLanguageDetector>();
        provider.GetRequiredService<ILogger>().Should().BeOfType<TraceLogger>();
        provider.GetRequiredService<II18Next>().Language.Should().Be("en-US");
        provider.GetRequiredService<IStringLocalizerFactory>().Should().BeOfType<I18NextStringLocalizerFactory>();
        provider.GetRequiredService<IStringLocalizer<ServiceCollectionExtensionsFixture>>().Should().NotBeNull();
        provider.GetRequiredService<IStringLocalizer>().Should().BeOfType<I18NextStringLocalizer>();
    }

    [Test]
    public void AddI18NextLocalization_Configured_ShouldTranslate()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(CreateBackend())
            .AddFormatter<UppercaseFormatter>()
            .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
            .UseDefaultLanguage("de")
            .UseDefaultNamespace("translation")
            .UseFallbackLanguage("en")
            .UseFallbackNamespace("common"));

        using var provider = services.BuildServiceProvider();
        var i18Next = provider.GetRequiredService<II18Next>();

        i18Next.T("exampleKey").Should().Be("Mein deutscher text.");
        i18Next.T("commonKey").Should().Be("Common English text.");
        i18Next.T("upper", new { value = "abc" }).Should().Be("ABC");
        i18Next.T("item", new { count = 1 }).Should().Be("ein Element");
        i18Next.T("item", new { count = 4 }).Should().Be("4 Elemente");

        provider.GetRequiredService<IStringLocalizer<ServiceCollectionExtensionsFixture>>()["exampleKey"].Value.Should().Be("Mein deutscher text.");
    }

    [Test]
    public void AddI18NextLocalization_GenericRegistrations_ShouldUseRegisteredTypes()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend<InMemoryBackend>()
            .AddLanguageDetector<ThreadLanguageDetector>()
            .AddLogger<TraceLogger>()
            .AddInterpolator<HtmlInterpolator>()
            .AddPostProcessor<SprintfPostProcessor>()
            .AddMissingKeyHandler(new CountingMissingKeyHandler())
            .Configure(o => o.DetectLanguageOnEachTranslation = true));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslationBackend>().Should().BeOfType<InMemoryBackend>();
        provider.GetRequiredService<ILanguageDetector>().Should().BeOfType<ThreadLanguageDetector>();
        provider.GetRequiredService<IInterpolator>().Should().BeOfType<HtmlInterpolator>();
        provider.GetRequiredService<II18Next>().DetectLanguageOnEachTranslation.Should().BeTrue();

        var translator = (DefaultTranslator) provider.GetRequiredService<ITranslator>();
        translator.PostProcessors.Should().ContainSingle().Which.Should().BeOfType<SprintfPostProcessor>();
        translator.MissingKeyHandlers.Should().ContainSingle();
    }

    [Test]
    public void AddI18NextLocalization_FactoryRegistrations_ShouldUseFactories()
    {
        var backend = CreateBackend();
        var logger = new TraceLogger();
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(_ => backend)
            .AddLanguageDetector(_ => new DefaultLanguageDetector("de"))
            .AddLogger(_ => logger)
            .AddFormatter(new LowercaseFormatter())
            .AddFormatter(_ => new UppercaseFormatter())
            .AddPluralResolver<DefaultPluralResolver>()
            .AddPostProcessor(new SprintfPostProcessor())
            .AddPostProcessor(_ => new IntervalPostProcessor())
            .AddMissingKeyHandler<CountingMissingKeyHandler>()
            .AddMissingKeyHandler(_ => new CountingMissingKeyHandler())
            .UseDefaultLanguage("de"));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslationBackend>().Should().BeSameAs(backend);
        provider.GetRequiredService<ILogger>().Should().BeSameAs(logger);
        provider.GetRequiredService<ILanguageDetector>().GetLanguage().Should().Be("de");

        var translator = (DefaultTranslator) provider.GetRequiredService<ITranslator>();
        translator.PostProcessors.Should().HaveCount(2);
        translator.MissingKeyHandlers.Should().HaveCount(2);

        provider.GetRequiredService<II18Next>().T("upper", new { value = "abc" }).Should().Be("ABC");
    }

    [Test]
    public void AddI18NextLocalization_CustomTranslator_ShouldUseTranslator()
    {
        var backend = CreateBackend();
        var translator = new DefaultTranslator(backend);

        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(backend).AddTranslator(translator));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslator>().Should().BeSameAs(translator);

        services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(backend).AddTranslator(_ => translator).AddInterpolator(_ => new HtmlInterpolator(new TraceLogger())));

        using var factoryProvider = services.BuildServiceProvider();

        factoryProvider.GetRequiredService<ITranslator>().Should().BeSameAs(translator);
        factoryProvider.GetRequiredService<IInterpolator>().Should().BeOfType<HtmlInterpolator>();

        services = new ServiceCollection();
        services.AddSingleton<ITranslationBackend>(backend);
        services.AddI18NextLocalization(i18n => i18n.AddTranslator<DefaultTranslator>());

        using var typeProvider = services.BuildServiceProvider();

        typeProvider.GetRequiredService<ITranslator>().Should().BeOfType<DefaultTranslator>();
    }

    [Test]
    public void StringLocalizerFactory_ShouldCreateLocalizers()
    {
        var backend = CreateBackend();
        var factory = new I18NextStringLocalizerFactory(new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de" });

        factory.Create(typeof(ServiceCollectionExtensionsFixture))["exampleKey"].Value.Should().Be("Mein deutscher text.");
        factory.Create("base", "location")["exampleKey"].Value.Should().Be("Mein deutscher text.");
    }

    [Test]
    public void Builder_InvalidArguments_ShouldThrow()
    {
        var builder = new I18NextBuilder(new ServiceCollection());

        builder.Invoking(b => b.UseDefaultLanguage("")).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.UseDefaultNamespace(null)).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.UseFallbackLanguage()).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.UseFallbackLanguage("en", "")).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.UseFallbackNamespace()).Should().Throw<ArgumentException>();
        builder.Invoking(b => b.UseFallbackNamespace("common", null)).Should().Throw<ArgumentException>();
    }

    private class CountingMissingKeyHandler : IMissingKeyHandler
    {
        public System.Threading.Tasks.Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
