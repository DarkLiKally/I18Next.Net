using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.AspNetCore;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Extensions.Builder;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.DataAnnotations.Tests;

public static class TestApplication
{
    public static InMemoryBackend CreateBackend()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "fields.email", "E-mail/address");
        backend.AddTranslation("en", "translation", "fields.confirmEmail", "E-mail confirmation");
        backend.AddTranslation("de", "translation", "Name", "Vorname");
        backend.AddTranslation("de", "translation", "Street", "Straße");
        backend.AddTranslation("de", "translation", "age", "Alter");
        backend.AddTranslation("de", "translation", "count", "Anzahl");
        backend.AddTranslation("de", "translation", "fields.email", "E-Mail-Adresse");
        backend.AddTranslation("de", "translation", "fields.confirmEmail", "E-Mail-Bestätigung");
        backend.AddTranslation("de", "translation", "fields.city", "Ort");
        backend.AddTranslation("en", "validation", "codeDigits", "{{- field}} may only contain digits.");
        backend.AddTranslation("de", "validation", "codeDigits", "{{- field}} darf nur Ziffern enthalten.");

        return backend;
    }

    public static async Task<WebApplication> StartAsync(Action<WebApplicationBuilder> configureServices, Action<WebApplication> configureApp,
        Action<I18NextBuilder> configureI18Next = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddI18NextLocalization(i18n =>
        {
            i18n.IntegrateToAspNetCore()
                .AddBackend(CreateBackend())
                .UseDefaultLanguage("en");

            configureI18Next?.Invoke(i18n);
        });

        configureServices(builder);

        var app = builder.Build();

        app.UseRequestLocalization(options => options
            .AddSupportedCultures("en", "de")
            .AddSupportedUICultures("en", "de")
            .SetDefaultCulture("en"));

        configureApp(app);

        await app.StartAsync();

        return app;
    }

    public static HttpRequestMessage CreateRequest(HttpMethod method, string uri, string language, string json = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Accept-Language", language);

        if (json != null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return request;
    }
}
