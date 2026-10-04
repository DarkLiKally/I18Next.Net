using System;
using System.Globalization;
using System.IO;

using I18Next.Net;
using I18Next.Net.AspNetCore;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales")))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));

var app = builder.Build();

app.UseRequestLocalization(options => options
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de")
    .SetDefaultCulture("en"));

// curl -H "Accept-Language: de" "http://localhost:5000/hello?name=Jane"
app.MapGet("/hello", (string name, II18Next i18n) => i18n.T("hello", new { name }));

app.MapGet("/orders/{count:int}", (int count, II18Next i18n) => i18n.T("orders", new { count }));

app.MapGet("/total/{value:double}", (double value, IStringLocalizer localizer) => localizer["total", new { value }].Value);

app.MapGet("/", (II18Next i18n) => new
{
    culture = CultureInfo.CurrentUICulture.Name,
    greeting = i18n.T("hello", new { name = "World" }),
    endpoints = new[] { "/hello?name=Jane", "/orders/3", "/total/42.5" }
});

app.Run();
