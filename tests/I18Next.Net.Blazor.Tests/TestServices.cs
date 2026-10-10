using System;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.Blazor.Tests;

internal static class TestServices
{
    public static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "title", "Welcome");
        backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}");
        backend.AddTranslation("en", "translation", "items_one", "{{count}} item");
        backend.AddTranslation("en", "translation", "items_other", "{{count}} items");
        backend.AddTranslation("en", "translation", "user.name", "Name");
        backend.AddTranslation("en", "translation", "user.role", "Role");
        backend.AddTranslation("en", "translation", "onlyEnglish", "Only in English");
        backend.AddTranslation("en", "common", "save", "Save");
        backend.AddTranslation("de", "translation", "title", "Willkommen");
        backend.AddTranslation("de", "translation", "greeting", "Hallo {{name}}");
        backend.AddTranslation("de", "translation", "items_one", "{{count}} Element");
        backend.AddTranslation("de", "translation", "items_other", "{{count}} Elemente");
        backend.AddTranslation("de", "common", "save", "Speichern");

        return backend;
    }

    public static IServiceCollection AddTestI18Next(this IServiceCollection services, ITranslationBackend backend = null, string detectedLanguage = "en",
        Action<I18NextBlazorOptions> configure = null)
    {
        return services.AddI18NextLocalization(i18n => i18n
            .IntegrateToBlazor(configure ?? (o => o.SupportedLanguages = ["en", "de"]))
            .AddBackend(backend ?? CreateBackend())
            .AddLanguageDetector(new DefaultLanguageDetector(detectedLanguage))
            .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
            .UseDefaultLanguage("en")
            .UseFallbackLanguage("en"));
    }
}
