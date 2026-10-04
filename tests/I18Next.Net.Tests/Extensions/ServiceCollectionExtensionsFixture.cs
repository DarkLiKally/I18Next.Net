using System;
using System.Linq;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Extensions.Builder;
using I18Next.Net.Formatters;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Extensions;

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

    [Fact]
    public void AddI18NextLocalization_Defaults_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<JsonFileBackend>();
        provider.GetRequiredService<ITranslator>().ShouldBeOfType<DefaultTranslator>();
        provider.GetRequiredService<IInterpolator>().ShouldBeOfType<DefaultInterpolator>();
        provider.GetRequiredService<IPluralResolver>().ShouldBeOfType<DefaultPluralResolver>();
        provider.GetRequiredService<ILanguageDetector>().ShouldBeOfType<DefaultLanguageDetector>();
        provider.GetRequiredService<ILogger>().ShouldBeOfType<TraceLogger>();
        provider.GetRequiredService<II18Next>().Language.ShouldBe("en-US");
        provider.GetRequiredService<IStringLocalizerFactory>().ShouldBeOfType<I18NextStringLocalizerFactory>();
        provider.GetRequiredService<IStringLocalizer<ServiceCollectionExtensionsFixture>>().ShouldNotBeNull();
        provider.GetRequiredService<IStringLocalizer>().ShouldBeOfType<I18NextStringLocalizer>();
    }

    [Fact]
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

        i18Next.T("exampleKey").ShouldBe("Mein deutscher text.");
        i18Next.T("commonKey").ShouldBe("Common English text.");
        i18Next.T("upper", new { value = "abc" }).ShouldBe("ABC");
        i18Next.T("item", new { count = 1 }).ShouldBe("ein Element");
        i18Next.T("item", new { count = 4 }).ShouldBe("4 Elemente");

        provider.GetRequiredService<IStringLocalizer<ServiceCollectionExtensionsFixture>>()["exampleKey"].Value.ShouldBe("Mein deutscher text.");
    }

    [Fact]
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

        provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<InMemoryBackend>();
        provider.GetRequiredService<ILanguageDetector>().ShouldBeOfType<ThreadLanguageDetector>();
        provider.GetRequiredService<IInterpolator>().ShouldBeOfType<HtmlInterpolator>();
        provider.GetRequiredService<II18Next>().DetectLanguageOnEachTranslation.ShouldBeTrue();

        var translator = (DefaultTranslator)provider.GetRequiredService<ITranslator>();
        translator.PostProcessors.ShouldHaveSingleItem().ShouldBeOfType<SprintfPostProcessor>();
        translator.MissingKeyHandlers.ShouldHaveSingleItem();
    }

    [Fact]
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

        provider.GetRequiredService<ITranslationBackend>().ShouldBeSameAs(backend);
        provider.GetRequiredService<ILogger>().ShouldBeSameAs(logger);
        provider.GetRequiredService<ILanguageDetector>().GetLanguage().ShouldBe("de");

        var translator = (DefaultTranslator)provider.GetRequiredService<ITranslator>();
        translator.PostProcessors.Count().ShouldBe(2);
        translator.MissingKeyHandlers.Count().ShouldBe(2);

        provider.GetRequiredService<II18Next>().T("upper", new { value = "abc" }).ShouldBe("ABC");
    }

    [Fact]
    public void AddI18NextLocalization_CustomTranslator_ShouldUseTranslator()
    {
        var backend = CreateBackend();
        var translator = new DefaultTranslator(backend);

        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(backend).AddTranslator(translator));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITranslator>().ShouldBeSameAs(translator);

        services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(backend).AddTranslator(_ => translator).AddInterpolator(_ => new HtmlInterpolator(new TraceLogger())));

        using var factoryProvider = services.BuildServiceProvider();

        factoryProvider.GetRequiredService<ITranslator>().ShouldBeSameAs(translator);
        factoryProvider.GetRequiredService<IInterpolator>().ShouldBeOfType<HtmlInterpolator>();

        services = new ServiceCollection();
        services.AddSingleton<ITranslationBackend>(backend);
        services.AddI18NextLocalization(i18n => i18n.AddTranslator<DefaultTranslator>());

        using var typeProvider = services.BuildServiceProvider();

        typeProvider.GetRequiredService<ITranslator>().ShouldBeOfType<DefaultTranslator>();
    }

    [Fact]
    public void StringLocalizerFactory_ShouldCreateLocalizers()
    {
        var backend = CreateBackend();
        var factory = new I18NextStringLocalizerFactory(new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de" });

        factory.Create(typeof(ServiceCollectionExtensionsFixture))["exampleKey"].Value.ShouldBe("Mein deutscher text.");
        factory.Create("base", "location")["exampleKey"].Value.ShouldBe("Mein deutscher text.");
    }

    [Fact]
    public void AddI18NextLocalization_LanguageFallbacks_ShouldBeApplied()
    {
        var backend = CreateBackend();
        backend.AddTranslation("fr", "translation", "frenchKey", "Texte français.");
        backend.AddTranslation("it", "translation", "italianKey", "Testo italiano.");

        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(backend)
            .UseFallbackLanguage("fr")
            .UseFallbackLanguagesFor("de-CH", "it"));

        using var provider = services.BuildServiceProvider();
        var i18Next = provider.GetRequiredService<II18Next>();

        i18Next.T("de-CH", "italianKey").ShouldBe("Testo italiano.");
        i18Next.T("de-CH", "frenchKey").ShouldBe("frenchKey");
        i18Next.T("de", "frenchKey").ShouldBe("Texte français.");
    }

    [Fact]
    public void Builder_InvalidArguments_ShouldThrow()
    {
        var builder = new I18NextBuilder(new ServiceCollection());

        Should.Throw<ArgumentException>(() => builder.UseDefaultLanguage(""));
        Should.Throw<ArgumentException>(() => builder.UseDefaultNamespace(null));
        Should.Throw<ArgumentException>(() => builder.UseFallbackLanguage());
        Should.Throw<ArgumentException>(() => builder.UseFallbackLanguage("en", ""));
        Should.Throw<ArgumentException>(() => builder.UseFallbackNamespace());
        Should.Throw<ArgumentException>(() => builder.UseFallbackNamespace("common", null));
        Should.Throw<ArgumentException>(() => builder.UseFallbackLanguagesFor("", "en"));
        Should.Throw<ArgumentException>(() => builder.UseFallbackLanguagesFor("de"));
        Should.Throw<ArgumentException>(() => builder.UseFallbackLanguagesFor("de", "en", null));
    }

    private class CountingMissingKeyHandler : IMissingKeyHandler
    {
        public System.Threading.Tasks.Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
