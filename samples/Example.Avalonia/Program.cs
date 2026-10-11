using System;
using System.IO;

using Avalonia;

using I18Next.Net;
using I18Next.Net.Avalonia;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;

namespace Example.Avalonia;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var locales = Path.Combine(AppContext.BaseDirectory, "locales");

        using var services = new ServiceCollection()
            .AddI18NextLocalization(i18n => i18n
                .AddBackend(new JsonFileBackend(locales))
                .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
                .UseDefaultLanguage("en")
                .UseFallbackLanguage("en")
                .WatchTranslationFiles(locales))
            .BuildServiceProvider();

        BuildAvaloniaApp()
            .UseI18Next(services.GetRequiredService<II18Next>())
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect();
    }
}
