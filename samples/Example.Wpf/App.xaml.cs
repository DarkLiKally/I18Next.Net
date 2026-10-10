using System;
using System.IO;
using System.Windows;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using I18Next.Net.Wpf;

using Microsoft.Extensions.DependencyInjection;

namespace Example.Wpf;

public partial class App
{
    private ServiceProvider _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        var locales = Path.Combine(AppContext.BaseDirectory, "locales");

        _services = new ServiceCollection()
            .AddI18NextLocalization(i18n => i18n
                .AddBackend(new JsonFileBackend(locales))
                .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
                .UseDefaultLanguage("en")
                .UseFallbackLanguage("en")
                .WatchTranslationFiles(locales))
            .BuildServiceProvider();

        I18NextXaml.Instance = _services.GetRequiredService<II18Next>();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();

        base.OnExit(e);
    }
}
