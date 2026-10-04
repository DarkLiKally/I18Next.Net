# I18Next.Net

[![build](https://github.com/DarkLiKally/I18Next.Net/actions/workflows/build.yml/badge.svg)](https://github.com/DarkLiKally/I18Next.Net/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/I18Next.Net.svg)](https://www.nuget.org/packages/I18Next.Net/)

I18Next.Net is a port of [i18next](https://www.i18next.com/) for .NET. It reads the same translation files as the
JavaScript library and implements most of its features, so web frontends and .NET backends can share their translations.
On top of that it integrates with `Microsoft.Extensions.DependencyInjection`, `IStringLocalizer` and ASP.NET Core view
localization.

- [Packages](#packages)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Translation files](#translation-files)
- [Usage](#usage)
- [Dependency injection and ASP.NET Core](#dependency-injection-and-aspnet-core)
- [Feature parity with i18next](#feature-parity-with-i18next)
- [Breaking changes](#breaking-changes)
- [Development](#development)

## Packages

| Package | Description |
|---|---|
| `I18Next.Net` | Core library with translator, interpolation, plurals, formats and the JSON, XML, INI and in-memory backends |
| `I18Next.Net.Abstractions` | Interfaces for writing your own backends, translators, interpolators, formatters and loggers |
| `I18Next.Net.Extensions` | Registration in `IServiceCollection` and `IStringLocalizer` support |
| `I18Next.Net.AspNetCore` | ASP.NET Core integration including view localization |
| `I18Next.Net.ICU` | Interpolator for ICU message format strings |
| `I18Next.Net.PolyglotJs` | Interpolator for Polyglot.js style translations |
| `I18Next.Net.Gettext` | Backend for gettext `.po` and `.mo` files |
| `I18Next.Net.Serilog` | Logger forwarding I18Next.Net log messages to Serilog |

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

## Translation files

The file backends look for `{basePath}/{language}/{namespace}.{extension}` and fall back to the language part of the
requested language (`locales/en` for `en-US`). The default namespace is `translation`, so `T("welcome")` reads
`locales/en/translation.json`. Other namespaces are selected with the `namespace:key` syntax.

```
locales/
├── en/
│   ├── translation.json
│   └── common.json
└── de/
    ├── translation.json
    └── common.json
```

```csharp
i18n.T("common:save");
i18n.T("de", "common", "save");
```

| Backend | Files |
|---|---|
| `JsonFileBackend` | i18next JSON files including nested objects and arrays (`key.0`) |
| `XmlFileBackend` | XML where elements form the keys (`<inbox><title>Inbox</title></inbox>`) |
| `StrictXmlFileBackend` | XML using `<Section name="...">` and `<Translation key="...">` elements |
| `IniFileBackend` | INI files where sections form the key prefix |
| `InMemoryBackend` | Translations added in code |
| `CompositeBackend` | Combines several backends, the first one providing a namespace wins |
| `GettextBackend` | `.po`/`.mo` files (`I18Next.Net.Gettext`) |

To use a different file layout override `FindFile`:

```csharp
public class FlatJsonFileBackend : JsonFileBackend
{
    public FlatJsonFileBackend(string basePath)
        : base(basePath)
    {
    }

    // locales/translation_en.json instead of locales/en/translation.json
    protected override string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, $"{@namespace}_{language}.json");

        return File.Exists(path) ? path : null;
    }
}
```

## Usage

The examples below use the JSON v4 plural format of current i18next versions:

```csharp
var logger = new TraceLogger();
var backend = new JsonFileBackend("locales");
var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));
var i18n = new I18NextNet(backend, translator) { Language = "en" };
```

### Languages

```csharp
i18n.Language = "de";
i18n.T("welcome");               // current language
i18n.T("fr", "welcome");         // explicit language
i18n.T("fr", "common", "save");  // explicit language and namespace
i18n.T("cimode", "welcome");     // translation:welcome, useful for tests and screenshots

i18n.LanguageChanged += (sender, e) => Console.WriteLine($"{e.OldLanguage} -> {e.NewLanguage}");

i18n.Dir("ar");                  // rtl
i18n.Dir();                      // text direction of the current language
```

### Interpolation

```json
{
    "greeting": "Hello {{name}}",
    "html": "Hello {{name}} and {{- raw}}",
    "nested": "Hello {{user.firstName}}"
}
```

```csharp
i18n.T("greeting", new { name = "Jane" });                         // Hello Jane
i18n.T("nested", new { user = new { firstName = "Jane" } });       // Hello Jane
i18n.T("greeting", new Dictionary<string, object> { ["name"] = "Jane" });
i18n.T("greeting", new { replace = new { name = "Jane" } });       // separate replacement values
i18n.T("greeting", new { interpolate = false });                   // Hello {{name}}
```

Values are escaped by the `HtmlInterpolator` (used by the ASP.NET Core integration). `{{- value}}` skips escaping. The
delimiters can be changed:

```csharp
var interpolator = new DefaultInterpolator(logger)
{
    Prefix = "[[",
    Suffix = "]]",
    UnescapePrefix = "!",
    NestingPrefix = "$t(",
    NestingSuffix = ")"
};
```

### Formatting

The i18next built-in formats are available without any registration. They use the culture of the target language and
the bundled [CLDR](https://cldr.unicode.org/) data, so the results match the browser `Intl` APIs used by i18next.

```json
{
    "price": "Price: {{value, currency(EUR)}}",
    "amount": "{{value, number(minimumFractionDigits: 2)}}",
    "date": "{{value, datetime(dateStyle: long; timeStyle: short)}}",
    "updated": "Updated {{value, relativetime(numeric: auto)}}",
    "inQuarters": "{{value, relativetime(quarter)}}",
    "people": "{{value, list}}",
    "choice": "{{value, list(type: disjunction)}}",
    "shout": "{{value, list, uppercase}}"
}
```

```csharp
i18n.T("price", new { value = 1234.5 });                  // Price: €1,234.50
i18n.T("de", "price", new { value = 1234.5 });            // Price: 1.234,50 €
i18n.T("amount", new { value = 5 });                      // 5.00
i18n.T("updated", new { value = -1 });                    // Updated yesterday
i18n.T("inQuarters", new { value = 2 });                  // in 2 quarters
i18n.T("people", new { value = new[] { "Anna", "Ben", "Carl" } });   // Anna, Ben, and Carl
i18n.T("choice", new { value = new[] { "tea", "coffee" } });         // tea or coffee
```

| Format | Options |
|---|---|
| `number` | `minimumFractionDigits`, `maximumFractionDigits`, `useGrouping` |
| `currency(EUR)` | currency code (positional or `currency`), `minimumFractionDigits`, `maximumFractionDigits` |
| `datetime` | `dateStyle` and `timeStyle` (`full`, `long`, `medium`, `short`) |
| `relativetime(day)` | unit (positional or `range`): `year`, `quarter`, `month`, `week`, `day`, `hour`, `minute`, `second`; `numeric` (`always`, `auto`); `style` (`long`, `short`, `narrow`) |
| `list` | `type` (`conjunction`, `disjunction`, `unit`), `style` (`long`, `short`, `narrow`) |

Formats are chained with the format separator (`{{value, number, uppercase}}`). Besides the i18next formats every
.NET format string works (`{{value, N2}}`, `{{value, #,##0.00}}`), as well as the bundled `LowercaseFormatter`,
`UppercaseFormatter` and the `MomentJsFormatter` (`{{date, dddd, MMMM Do}}`). Custom formatters implement `IFormatter`:

```csharp
public class ReverseFormatter : IFormatter
{
    public bool CanFormat(object value, string format, string language) => format == "reverse";

    public string Format(object value, string format, string language) => new string(value.ToString().Reverse().ToArray());
}

interpolator.Formatters.Add(new ReverseFormatter());
interpolator.ChainableFormats.Add("reverse"); // allows {{value, reverse, uppercase}}
```

### Nesting

```json
{
    "app": "I18Next.Net",
    "welcome": "Welcome to $t(app)",
    "items": "$t(item, {\"count\": {{amount}} })",
    "item_one": "{{count}} item",
    "item_other": "{{count}} items"
}
```

```csharp
i18n.T("welcome");                      // Welcome to I18Next.Net
i18n.T("items", new { amount = 3 });    // 3 items
```

### Plurals

Plurals use the `count` argument. The JSON format version of the plural resolver decides about the suffixes.
`Version4` is the format of current i18next versions and uses the CLDR plural categories, `Version3` is the default for
compatibility with existing translation files.

```json
{
    "item_zero": "No items",
    "item_one": "{{count}} item",
    "item_other": "{{count}} items",
    "place_ordinal_one": "{{count}}st place",
    "place_ordinal_two": "{{count}}nd place",
    "place_ordinal_few": "{{count}}rd place",
    "place_ordinal_other": "{{count}}th place"
}
```

```csharp
i18n.T("item", new { count = 0 });                    // No items
i18n.T("item", new { count = 1 });                    // 1 item
i18n.T("item", new { count = 5 });                    // 5 items
i18n.T("place", new { count = 22, ordinal = true });  // 22nd place
```

| Format | Example keys |
|---|---|
| `Version1` | `key`, `key_plural`, `key_plural_2`, `key_plural_5` |
| `Version2` | `key`, `key_plural`, `key_1`, `key_2`, `key_5` |
| `Version3` | `key`, `key_plural`, `key_0`, `key_1`, `key_2` |
| `Version4` | `key_zero`, `key_one`, `key_two`, `key_few`, `key_many`, `key_other`, `key_ordinal_one` |

The rules of all 224 CLDR languages for cardinals and 108 for ordinals are included and verified against the CLDR
samples.

### Context

```json
{
    "friend": "A friend",
    "friend_male": "A boyfriend",
    "friend_female": "A girlfriend",
    "friend_male_other": "{{count}} boyfriends"
}
```

```csharp
i18n.T("friend", new { context = "male" });               // A boyfriend
i18n.T("friend", new { context = "male", count = 2 });    // 2 boyfriends
i18n.T("friend", new { context = "unknown" });            // A friend
```

### Default values and multiple keys

```csharp
i18n.T("missing", new { defaultValue = "Fallback for {{name}}", name = "Jane" });   // Fallback for Jane
i18n.T("missing", new Dictionary<string, object>
{
    ["count"] = 2,
    ["defaultValue_one"] = "{{count}} item",
    ["defaultValue_other"] = "{{count}} items"
});                                                                                 // 2 items

i18n.T(new[] { "error.404", "error.unspecific" });   // first key that exists
i18n.Exists("error.404");                            // true or false, without triggering missing key handlers
```

### Objects and arrays

```json
{
    "menu": {
        "title": "Hello {{name}}",
        "items": [ "Home", "About" ]
    }
}
```

```csharp
public class Menu
{
    public string Title { get; set; }
    public string[] Items { get; set; }
}

var menu = i18n.T<Menu>("menu", new { name = "Jane" });   // Title = "Hello Jane", Items = ["Home", "About"]
var values = i18n.TObject("menu", new { name = "Jane" }); // nested IDictionary<string, object>, arrays as object[]
var items = i18n.T<string[]>("menu.items");
i18n.T("menu.items", new { joinArrays = ", " });          // Home, About
i18n.T("menu.items.0");                                   // Home
```

All values of an object are interpolated with the given arguments.

### Fallbacks

```csharp
i18n.SetFallbackLanguages("en");                // for every language
i18n.SetLanguageFallbacks("de-CH", "fr", "en"); // only for de-CH, takes precedence
i18n.SetLanguageFallbacks("pt", "es");          // for pt and all regions of it, e.g. pt-BR
i18n.SetFallbackNamespaces("common");
```

Fallback namespaces are checked first in the requested language, then the fallback languages are checked with the
requested and the fallback namespaces.

### Language detection

```csharp
var i18n = new I18NextNet(backend, translator, new ThreadLanguageDetector())
{
    DetectLanguageOnEachTranslation = true
};

CultureInfo.CurrentCulture = new CultureInfo("de-DE");
i18n.T("welcome");      // translated to de-DE without changing i18n.Language

i18n.UseDetectedLanguage();   // sets i18n.Language to the detected language
```

### Missing keys

```csharp
translator.MissingKey += (sender, e) => Console.WriteLine($"{e.Language} {e.Namespace}:{e.Key} ({string.Join(", ", e.PossibleKeys)})");
translator.MissingKeyHandlers.Add(new MyMissingKeyHandler());   // IMissingKeyHandler, e.g. to report missing keys
```

### Post processors

```csharp
translator.PostProcessors.Add(new SprintfPostProcessor());
translator.PostProcessors.Add(new IntervalPostProcessor());

var pseudoOptions = new PseudoLocalizationOptions();
pseudoOptions.LanguagesToPseudo.Add("en");
translator.PostProcessors.Add(new PseudoLocalizationPostProcessor(pseudoOptions));
```

```json
{
    "sprintf": "The first letters are %s, %s and %s",
    "interval": "(1){one item};(2-7){a few items};(8-inf){a lot of items};"
}
```

```csharp
i18n.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "a", "b", "c" } });   // The first letters are a, b and c
i18n.T("interval", new { postProcess = "interval", count = 3 });                          // a few items
i18n.T("welcome", new { postProcess = "pseudo", name = "Jane" });                          // Ḥḛḛḽḽṓṓ Ĵααṇḛḛ!
```

### Keys and namespaces

```csharp
translator.NamespaceSeparator = "::";    // other::key, null disables namespaces in keys
translator.ContextSeparator = "_";

// Keys containing dots, the equivalent of keySeparator: false
var backend = new JsonFileBackend("locales", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
```

### Resources at runtime

```csharp
var backend = new InMemoryBackend();
backend.AddTranslation("en", "translation", "key", "value");
backend.AddTranslations("de", "translation", new Dictionary<string, string> { ["key"] = "Wert" });
backend.HasNamespace("de", "translation");
backend.RemoveNamespace("de", "translation");

translator.ClearCache();                          // reload all namespaces on the next translation
translator.ClearCache("de", "translation");       // reload one namespace
```

### Logging

The default `TraceLogger` writes warnings and errors to `System.Diagnostics.Trace`. With dependency injection an
existing `Microsoft.Extensions.Logging.ILogger` is used automatically. Serilog is supported by `I18NextSerilogLogger`.

```csharp
var logger = new TraceLogger { LogLevel = LogLevel.Debug };
var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver(), new DefaultInterpolator(logger));
```

## Dependency injection and ASP.NET Core

```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend("wwwroot/locales"))
    .AddPluralResolver(new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 })
    .AddFormatter<MomentJsFormatter>()
    .AddPostProcessor<SprintfPostProcessor>()
    .UseDefaultLanguage("en")
    .UseDefaultNamespace("translation")
    .UseFallbackLanguage("en")
    .UseFallbackLanguagesFor("de-CH", "fr")
    .UseFallbackNamespace("common"));

services.AddControllersWithViews()
    .AddI18NextViewLocalization();
```

`IntegrateToAspNetCore` registers the `HtmlInterpolator` and detects the language of every request from the current
culture. Enable request localization to set the culture from the `Accept-Language` header:

```csharp
app.UseRequestLocalization(options => options.AddSupportedCultures("de", "en"));
```

Inject `II18Next`, `IStringLocalizer`, `IStringLocalizer<T>` or `IViewLocalizer`:

```csharp
public class HomeController : Controller
{
    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly II18Next _i18n;

    public HomeController(IStringLocalizer<HomeController> localizer, II18Next i18n)
    {
        _localizer = localizer;
        _i18n = i18n;
    }

    public IActionResult About()
    {
        ViewData["Message"] = _localizer["about.description"];
        ViewData["Items"] = _i18n.T("cart.items", new { count = 3 });

        var missing = _localizer["unknown"].ResourceNotFound;   // true

        return View();
    }
}
```

```cshtml
@inject IViewLocalizer Localizer

<h1>@Localizer["about.title"]</h1>
```

## Feature parity with i18next

| i18next feature | Status | Notes |
|---|:---:|---|
| Basic translation `t(key)` | ✅ | `T`, `Ta` |
| Namespaces `ns:key` | ✅ | Configurable separator |
| Nested keys `a.b.c` | ✅ | `FlatTranslationTreeBuilder` for `keySeparator: false` |
| Interpolation `{{value}}`, `{{obj.prop}}` | ✅ | |
| Unescaped interpolation `{{- value}}` | ✅ | |
| Escaping | ✅ | `HtmlInterpolator` |
| Custom prefix/suffix | ✅ | `Prefix`, `Suffix`, `UnescapePrefix` |
| `skipOnVariables`, `alwaysFormat` | ❌ | |
| Formatting with format strings | ✅ | .NET format strings and MomentJS tokens |
| Built-in `number`, `currency`, `datetime` | ✅ | `datetime` maps the styles to the culture patterns |
| Built-in `relativetime`, `list` | ✅ | Bundled CLDR 48.2 data |
| Chained formats | ✅ | |
| Custom formatters | ✅ | `IFormatter` |
| `formatParams` per call | ❌ | Use the inline options |
| Nesting `$t(key)`, `$t(key, {...})` | ✅ | Custom nesting prefix/suffix |
| Plurals JSON v1, v2, v3 | ✅ | |
| Plurals JSON v4 | ✅ | CLDR categories incl. `_zero` lookup |
| Ordinal plurals | ✅ | `ordinal = true` |
| Plurals with decimal counts | ❌ | `count` must be an integer |
| Context | ✅ | Including plural combinations |
| `defaultValue` incl. plural variants | ✅ | |
| Multiple fallback keys `t([...])` | ✅ | |
| `exists` | ✅ | |
| `returnObjects` | ✅ | `TObject`, `T<TModel>` |
| `joinArrays` | ✅ | |
| Arrays in resources | ✅ | `key.0` |
| `returnNull`, `returnEmptyString` | ⚠️ | `null` values are skipped, empty strings are returned |
| Fallback languages | ✅ | |
| Fallback languages per language | ✅ | `SetLanguageFallbacks`, `UseFallbackLanguagesFor` |
| Fallback from region to language (`de-CH` → `de`) | ✅ | Done by the backends |
| Fallback namespaces | ✅ | |
| `cimode` | ✅ | |
| `dir` | ✅ | `Dir()` |
| `changeLanguage`, `languageChanged` event | ✅ | `Language` setter, `LanguageChanged` |
| Language detection | ✅ | `ILanguageDetector`, `ThreadLanguageDetector`, ASP.NET Core request culture |
| Missing key handling | ✅ | `MissingKey` event, `IMissingKeyHandler` |
| `saveMissing` to backend | ❌ | Implement an `IMissingKeyHandler` |
| Post processors | ✅ | sprintf, interval, pseudo localization, custom `IPostProcessor` |
| Backends | ✅ | JSON, XML, INI, gettext, in-memory, composite, custom `ITranslationBackend` |
| `addResource`, `addResourceBundle`, `hasResourceBundle`, `removeResourceBundle` | ✅ | `InMemoryBackend` |
| `reloadResources` | ✅ | `DefaultTranslator.ClearCache` |
| `getFixedT` | ❌ | Use the language and namespace overloads of `T` |
| `keyPrefix` | ❌ | |
| ICU message format | ✅ | `I18Next.Net.ICU` |
| Logging | ✅ | `TraceLogger`, Microsoft.Extensions.Logging, Serilog |

## Breaking changes

Compared to version 1.0.0:

- `ITranslator` and `II18Next` have new members (`ExistsAsync`, `TranslateObjectAsync`, `TObject`, `T<TModel>`, ...).
  Custom implementations have to add them.
- `Newtonsoft.Json` was replaced by `System.Text.Json`.
- `TranslationTree.GetAllValues()` returns the full key paths (`look.deep` instead of `deep`).
- Translating a key which leads to an object returns i18next's
  `key '...' returned an object instead of string.` message instead of throwing an exception.
- JSON v1 plural suffixes for numbers use `_plural_N` like i18next and negative counts use the absolute value.
- `TraceLogger` respects its `LogLevel`.
- .NET Standard 2.1 and .NET 5 are no longer separate targets, they use the .NET Standard 2.0 build.

## Development

```
dotnet build
dotnet test tests/I18Next.Net.Tests
dotnet run -c Release --project tests/I18Next.Net.Benchmarks
```

The CLDR data for relative times, lists and the plural rule tests is generated from the official CLDR JSON packages:

```
python3 tools/cldr/generate.py
```

## License

[Apache License 2.0](LICENSE)
