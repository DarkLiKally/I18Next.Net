using System.IO;

using Example.Blazor.Components;

using I18Next.Net.Backends;
using I18Next.Net.Blazor;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var locales = Path.Combine(builder.Environment.WebRootPath, "locales");

builder.Services.AddI18NextLocalization(i18n =>
{
    i18n.IntegrateToBlazor(o => o.SupportedLanguages = ["en", "de"])
        .AddBackend(new JsonFileBackend(locales))
        .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
        .UseDefaultLanguage("en")
        .UseFallbackLanguage("en");

    if (builder.Environment.IsDevelopment())
        i18n.WatchTranslationFiles(locales);
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();

app.UseRequestLocalization(options => options
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de")
    .SetDefaultCulture("en"));

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Example.Blazor.Client._Imports).Assembly);

app.Run();
