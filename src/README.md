# I18Next.Net

I18Next.Net is a port of [i18next](https://www.i18next.com/) for .NET. It reads the same translation files as the
JavaScript library and implements its features, so web frontends and .NET backends can share their translations. It
integrates with `Microsoft.Extensions.DependencyInjection`, `IStringLocalizer` and ASP.NET Core.

**Documentation:** [darklikally.github.io/I18Next.Net](https://darklikally.github.io/I18Next.Net/)

## Packages

| Package | Description |
|---|---|
| `I18Next.Net` | Translator, interpolation, plurals, formats and the JSON, XML, INI, HTTP, delegate, in-memory and chained backends |
| `I18Next.Net.Abstractions` | Interfaces for custom backends, translators, interpolators, formatters and loggers |
| `I18Next.Net.Extensions` | Registration in `IServiceCollection` and `IStringLocalizer` support |
| `I18Next.Net.AspNetCore` | ASP.NET Core integration including request language detection, view localization, a translations endpoint for i18next in the browser and localized routes |
| `I18Next.Net.Blazor` | Blazor components and a language per user |
| `I18Next.Net.Wpf` | WPF markup extension `{i18n:T key}` updating on language and translation changes |
| `I18Next.Net.Maui` | .NET MAUI markup extension `{i18n:T key}` |
| `I18Next.Net.DataAnnotations` | Translated DataAnnotations messages and display names |
| `I18Next.Net.FluentValidation` | FluentValidation messages from i18next |
| `I18Next.Net.EntityFrameworkCore` | Backend and missing key handler storing translations in a database |
| `I18Next.Net.MachineTranslation` | Machine translation with DeepL, Azure AI Translator or your own delegate |
| `I18Next.Net.Tool` | The `dotnet i18next` tool to extract, check, sort, convert, migrate and machine translate translation files |
| `I18Next.Net.Generators` | Source generator for typed keys and translation methods, analyzers and code fixes, Native AOT support |
| `I18Next.Net.Yaml` | Backend for YAML translation files |
| `I18Next.Net.Gettext` | Backend for gettext `.mo` files |
| `I18Next.Net.ICU` | Interpolator for ICU message format strings |
| `I18Next.Net.PolyglotJs` | Interpolator for Polyglot.js style phrases |
| `I18Next.Net.Serilog` | Logger forwarding the log messages to Serilog |

## Quick start

```json
// locales/en/translation.json
{
    "welcome": "Hello {{name}}!",
    "item_one": "{{count}} item",
    "item_other": "{{count}} items",
    "price": "Price: {{value, currency(EUR)}}"
}
```

```csharp
var backend = new JsonFileBackend("locales");
var translator = new DefaultTranslator(backend, new TraceLogger(),
    new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 }, new DefaultInterpolator(new TraceLogger()));
var i18n = new I18NextNet(backend, translator) { Language = "en" };

i18n.T("welcome", new { name = "Jane" });   // Hello Jane!
i18n.T("item", new { count = 5 });          // 5 items
i18n.T("price", new { value = 1234.5 });    // Price: €1,234.50
await i18n.Ta("welcome", new { name = "Jane" });
```

With dependency injection and ASP.NET Core:

```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend("wwwroot/locales"))
    .UseDefaultLanguage("en")
    .UseFallbackLanguage("en"));
```

## Features

- i18next JSON v1 to v4 plurals with the CLDR rules of all languages, ordinals and decimal counts
- Interpolation, nesting, context, default values, fallback languages and namespaces, objects and arrays
- `number`, `currency`, `datetime`, `relativetime` and `list` formats matching the browser `Intl` APIs, date-fns and
  Moment.js formats
- `getFixedT`, `keyPrefix`, `formatParams`, `returnObjects`, `joinArrays` and post processors
- JSON, YAML, XML, INI, gettext, HTTP, delegate, in-memory, distributed cache and Entity Framework Core backends, chained
  with caching and expiry
- Hot reload of changed translation files and saving missing keys to files, over HTTP or as metrics
- Typed keys and translation methods generated from the translation files
- Blazor, WPF, .NET MAUI, DataAnnotations and FluentValidation integrations
- Trimming and Native AOT support
- .NET Standard 2.0, .NET 6, .NET 8 and .NET 10

See the [documentation](https://darklikally.github.io/I18Next.Net/) for all features, the
[feature parity with i18next](https://darklikally.github.io/I18Next.Net/reference/parity) and the
[samples](https://github.com/DarkLiKally/I18Next.Net/tree/master/samples).
