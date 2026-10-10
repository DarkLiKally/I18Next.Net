using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net;
using I18Next.Net.AspNetCore;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales")))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .AddMissingKeyHandler<ConsoleMissingKeyHandler>()
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));

var app = builder.Build();

app.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en", "de")
        .AddSupportedUICultures("en", "de")
        .SetDefaultCulture("en");
    options.RequestCultureProviders.Insert(0, new PathLanguageRequestCultureProvider());
});

// curl http://localhost:5000/de/produkte/42 is served by /products/{id}, the segments are translated in locales/de/routes.json
app.UseI18NextLocalizedRoutes(options => options.Languages = ["en", "de"]);

app.UseRouting();

// curl -H "Accept-Language: de" "http://localhost:5000/hello?name=Jane"
app.MapGet("/hello", (string name, II18Next i18n) => i18n.T("hello", new { name }));

app.MapGet("/orders/{count:int}", (int count, II18Next i18n) => i18n.T("orders", new { count }));

app.MapGet("/total/{value:double}", (double value, IStringLocalizer localizer) => localizer["total", new { value }].Value);

app.MapGet("/products/{id:int}", (int id, HttpContext context, II18Next i18n) => new
{
    title = i18n.T("product", new { id }),
    links = new
    {
        en = context.GetLocalizedRequestPath("en"),
        de = context.GetLocalizedRequestPath("de"),
        orders = context.GetLocalizedPath("/orders/3")
    }
});

// curl http://localhost:5000/locales/de/translation.json serves the translations to i18next with the i18next-http-backend
app.MapI18NextResources(configure: options =>
{
    options.Languages = ["en", "de"];
    options.Namespaces = ["translation"];
});

if (app.Environment.IsDevelopment())
{
    // curl -H "Content-Type: application/json" -d '{"newKey":"New key"}' http://localhost:5000/locales/add/de/translation
    app.MapI18NextMissingKeys(configure: options =>
    {
        options.Languages = ["en", "de"];
        options.Namespaces = ["translation"];
    });
}

app.MapGet("/", (II18Next i18n) => new
{
    culture = CultureInfo.CurrentUICulture.Name,
    greeting = i18n.T("hello", new { name = "World" }),
    endpoints = new[]
    {
        "/hello?name=Jane", "/orders/3", "/total/42.5", "/products/42", "/de/produkte/42", "/de/bestellungen/3", "/locales/de/translation.json"
    }
});

app.Run();

internal class ConsoleMissingKeyHandler : IMissingKeyHandler
{
    public Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        Console.WriteLine($"Missing key {args.Language}/{args.Namespace}: {args.Key}");

        return Task.CompletedTask;
    }
}
