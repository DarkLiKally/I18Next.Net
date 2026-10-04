# Getting started

## Packages

| Package | Description |
|---|---|
| `I18Next.Net` | Core library with translator, interpolation, plurals, formats and the JSON, XML, INI, HTTP, in-memory, delegate and chained backends |
| `I18Next.Net.Abstractions` | Interfaces for writing your own backends, translators, interpolators, formatters and loggers |
| `I18Next.Net.Extensions` | Registration in `IServiceCollection` and `IStringLocalizer` support |
| `I18Next.Net.AspNetCore` | ASP.NET Core integration including view localization |
| `I18Next.Net.ICU` | Interpolator for ICU message format strings |
| `I18Next.Net.PolyglotJs` | Interpolator for Polyglot.js style translations |
| `I18Next.Net.Gettext` | Backend for gettext `.mo` files |
| `I18Next.Net.Yaml` | Backend and reader for YAML translation files |
| `I18Next.Net.Serilog` | Logger forwarding I18Next.Net log messages to Serilog |
| `I18Next.Net.Generators` | Source generator for typed keys and translation methods, plus analyzers for the translation files |

The packages target .NET Standard 2.0 (including .NET Framework 4.6.2 and later), .NET 6, .NET 8 and .NET 10. A .NET 11
build is added automatically when building with a .NET 11 SDK.

## Installation

```
dotnet add package I18Next.Net
dotnet add package I18Next.Net.Extensions
dotnet add package I18Next.Net.AspNetCore
```

## Quick start

```json
// locales/en/translation.json
{
    "welcome": "Hello {{name}}!",
    "inbox": {
        "title": "Inbox"
    }
}
```

```csharp
var backend = new JsonFileBackend("locales");
var translator = new DefaultTranslator(backend);
var i18n = new I18NextNet(backend, translator) { Language = "en" };

i18n.T("welcome", new { name = "Jane" });   // Hello Jane!
i18n.T("inbox.title");                      // Inbox
await i18n.Ta("welcome", new { name = "Jane" });
```
