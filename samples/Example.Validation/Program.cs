using System;
using System.IO;

using Example.Validation;

using FluentValidation;

using I18Next.Net.AspNetCore;
using I18Next.Net.Backends;
using I18Next.Net.DataAnnotations;
using I18Next.Net.Extensions;
using I18Next.Net.FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "locales")))
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en")
    .AddDataAnnotationsLocalization()
    .AddFluentValidationLocalization());

builder.Services.AddControllers().AddI18NextDataAnnotationsLocalization();
builder.Services.AddValidation();
builder.Services.AddSingleton<IValidator<Order>, OrderValidator>();

var app = builder.Build();

app.UseRequestLocalization(options => options
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de")
    .SetDefaultCulture("en"));

app.MapControllers();

// curl -X POST -H "Accept-Language: de" -H "Content-Type: application/json" -d '{"name":"J","email":"jane"}' http://localhost:5000/customers
app.MapPost("/customers", (Customer customer) => Results.Ok(customer));

// curl -X POST -H "Accept-Language: de" -H "Content-Type: application/json" -d '{"quantity":0}' http://localhost:5000/orders
app.MapPost("/orders", (Order order, IValidator<Order> validator) =>
{
    var result = validator.Validate(order);

    return result.IsValid ? Results.Ok(order) : Results.ValidationProblem(result.ToDictionary());
});

app.MapGet("/", () => new
{
    endpoints = new[] { "POST /customers", "POST /mvc/customers", "GET /mvc/customers?page=abc", "POST /orders" }
});

app.Run();
