using System;

using I18Next.Net.Blazor;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToBlazor(o => o.SupportedLanguages = ["en", "de"])
    .AddHttpBackend(configureHttpClient: c => c.ConfigureHttpClient(h => h.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));

var host = builder.Build();

await host.Services.GetRequiredService<IBlazorI18Next>().InitializeAsync();
await host.RunAsync();
