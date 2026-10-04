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
- [Built-in plugins](#built-in-plugins)
- [Typed keys with the source generator](#typed-keys-with-the-source-generator)
- [Dependency injection and ASP.NET Core](#dependency-injection-and-aspnet-core)
- [Feature parity with i18next](#feature-parity-with-i18next)
- [Breaking changes](#breaking-changes)
- [Performance](#performance)
- [Samples](#samples)
- [Development](#development)

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

All shipped backends are listed under [Backends](#backends). To use a different file layout override `FindFile`:

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
the bundled [CLDR](https://cldr.unicode.org/) 47 data, so the results match the browser `Intl` APIs used by i18next.

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
| `datetime` | `dateStyle`, `timeStyle` (`full`, `long`, `medium`, `short`), `weekday`, `era`, `year`, `month`, `day`, `hour`, `minute`, `second`, `fractionalSecondDigits`, `timeZoneName`, `hour12`, `hourCycle` |
| `relativetime(day)` | unit (positional or `range`): `year`, `quarter`, `month`, `week`, `day`, `hour`, `minute`, `second`; `numeric` (`always`, `auto`); `style` (`long`, `short`, `narrow`) |
| `list` | `type` (`conjunction`, `disjunction`, `unit`), `style` (`long`, `short`, `narrow`) |

Formats are chained with the format separator (`{{value, number, uppercase}}`). Besides the i18next formats every
.NET format string works (`{{value, N2}}`, `{{value, #,##0.00}}`), and date-fns or Moment.js patterns are supported by
the [bundled formatters](#formatters). Custom formatters implement `IFormatter`:

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

### Fixed translators and key prefixes

```csharp
var t = i18n.GetFixedT("de", "common", keyPrefix: "menu");
t.T("save");                                              // common:menu.save in German
t.T("translation:title");                                 // other namespaces still work

i18n.T("title", new { keyPrefix = "menu" });              // menu.title
```

### Format parameters

```json
{
    "price": "{{value, number}}"
}
```

```csharp
i18n.T("price", new { value = 5, formatParams = new { value = new { minimumFractionDigits = 2 } } });   // 5.00
```

### Interpolation options

```csharp
translator.ReturnEmptyString = false;   // empty strings fall back to other languages and default values
interpolator.SkipOnVariables = true;    // default: values containing {{...}} or $t(...) are not processed again
interpolator.AlwaysFormat = true;       // values without a format are passed to the formatters, too
```

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

## Built-in plugins

Everything below ships with the packages and can be combined freely. Each plugin type has an interface in
`I18Next.Net.Abstractions`, so any of them can be replaced by your own implementation.

### Backends

Backends load the translations of a language and namespace (`ITranslationBackend`). The file and HTTP backends fall back
to the language part of a regional language (`de` for `de-CH`).

| Backend | Package | Loads |
|---|---|---|
| `JsonFileBackend` | `I18Next.Net` | i18next JSON files from `{basePath}/{lng}/{ns}.json`, nested objects and arrays (`key.0`) |
| `XmlFileBackend` | `I18Next.Net` | XML files where elements form the keys (`<inbox><title>Inbox</title></inbox>`) |
| `StrictXmlFileBackend` | `I18Next.Net` | XML files with `<Section name="...">` and `<Translation key="...">` elements |
| `IniFileBackend` | `I18Next.Net` | INI files where sections form the key prefix |
| `HttpBackend` | `I18Next.Net` | JSON over HTTP, the equivalent of i18next-http-backend |
| `FuncBackend` | `I18Next.Net` | Whatever a delegate returns, the equivalent of i18next-resources-to-backend |
| `InMemoryBackend` | `I18Next.Net` | Translations added in code |
| `ChainedBackend` | `I18Next.Net` | Asks several backends in order, with optional caching and expiry |
| `GettextBackend` | `I18Next.Net.Gettext` | Compiled gettext `.mo` files from `{basePath}/{lng}/{ns}.mo` |
| `YamlFileBackend` | `I18Next.Net.Yaml` | YAML files from `{basePath}/{lng}/{ns}.yaml` or `.yml`, mappings and sequences like the JSON backend |

`YamlTranslationReader` parses YAML for the other backends:

```csharp
var http = new HttpBackend(httpClient, "locales/{{lng}}/{{ns}}.yaml") { Parse = YamlTranslationReader.Read };
var func = new FuncBackend((lng, ns) => YamlTranslationReader.Read(LoadYaml(lng, ns), ns));
```

`CompositeBackend` is the former name of `ChainedBackend` and still works, but is marked obsolete.

#### HttpBackend

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://cdn.example.com/") };
var backend = new HttpBackend(httpClient, "locales/{{lng}}/{{ns}}.json")
{
    LoadPathResolver = (lng, ns) => ns == "legal" ? $"https://legal.example.com/{lng}.json" : null,
    QueryStringParams = { ["v"] = "1.4.2" },
    CustomHeaders = { ["Authorization"] = "Bearer ..." },
    FallbackToLanguagePart = true
};
```

`{{lng}}` and `{{ns}}` are replaced in the load path, relative paths use the base address of the client. A `404` means
the namespace does not exist, other failed responses throw an `HttpRequestException`. `Parse` replaces the JSON parser,
e.g. for YAML or other formats. With dependency injection the client comes from `IHttpClientFactory`, so retries and
other resilience handlers can be added to it:

```csharp
services.AddI18NextLocalization(i18n => i18n
    .AddHttpBackend("locales/{{lng}}/{{ns}}.json",
        backend => backend.QueryStringParams["v"] = "1.4.2",
        client => client.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://cdn.example.com/"))));
```

#### FuncBackend

```csharp
// translation trees
new FuncBackend((lng, ns) => LoadTree(lng, ns));
new FuncBackend(async (lng, ns) => await LoadTreeAsync(lng, ns));

// JSON strings, e.g. from a database
FuncBackend.FromJson(async (lng, ns) => await db.GetTranslationJsonAsync(lng, ns));

// JSON streams, e.g. embedded resources
FuncBackend.FromStream((lng, ns) => typeof(Program).Assembly.GetManifestResourceStream($"MyApp.Locales.{lng}.{ns}.json"));

// nested dictionaries, lists, anonymous objects or JsonElements
FuncBackend.FromObject((lng, ns) => new { greeting = "Hello {{name}}", menu = new { items = new[] { "Home", "About" } } });
```

Returning `null` means the namespace does not exist.

#### ChainedBackend

```csharp
var backend = new ChainedBackend(
    new JsonFileBackend("overrides"),
    new HttpBackend(httpClient))
{
    CacheEnabled = true,
    CacheExpiration = TimeSpan.FromMinutes(10),
    UseExpiredCacheOnFailure = true
};
```

The first backend providing a namespace wins. `CacheEnabled` keeps loaded namespaces in memory, which helps when several
translators share the backend. `CacheExpiration` also tells the translator to reload a namespace after that time, so
updated translations are picked up while the application runs. When reloading fails or no backend provides the
namespace anymore, the expired namespace is used until the next expiry. `ClearCache()` and `ClearCache(lng, ns)` drop
cached namespaces.

### Translation tree builders

The file, HTTP and delegate backends accept an `ITranslationTreeBuilderFactory`:

| Builder | Description |
|---|---|
| `HierarchicalTranslationTreeBuilder` | Default. Splits keys at dots into groups, supports objects and arrays |
| `FlatTranslationTreeBuilder` | Keeps keys as they are, the equivalent of `keySeparator: false` |

```csharp
var backend = new JsonFileBackend("locales", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
```

### Interpolators

| Interpolator | Package | Description |
|---|---|---|
| `DefaultInterpolator` | `I18Next.Net` | i18next `{{value}}` interpolation, `$t()` nesting, formats, configurable delimiters |
| `HtmlInterpolator` | `I18Next.Net` | Like the default interpolator but HTML encodes values, used by the ASP.NET Core integration |
| `MessageFormatInterpolator` | `I18Next.Net.ICU` | ICU message format (`{count, plural, one {# item} other {# items}}`) |
| `PolyglotInterpolator` | `I18Next.Net.PolyglotJs` | Polyglot.js phrases (`%{name}`, plurals separated by `\|\|\|\|` and `smart_count`) |

```csharp
var translator = new DefaultTranslator(backend, new MessageFormatInterpolator());
```

### Formatters

Formatters are added to `DefaultInterpolator.Formatters` (or with `AddFormatter` when using dependency injection). The
first formatter whose `CanFormat` returns `true` formats the value, the `DefaultFormatter` is used when none matches.

| Formatter | Formats | Example |
|---|---|---|
| `DefaultFormatter` | Always active. The i18next formats via `IntlFormatter`, otherwise .NET format strings with the culture of the language | `{{value, N2}}` |
| `IntlFormatter` | `number`, `currency`, `datetime`, `relativetime` and `list` like the browser `Intl` APIs | `{{value, currency(EUR)}}` |
| `DateFnsFormatter` | `DateTime` and `DateTimeOffset` with date-fns `format` tokens, all date-fns locales bundled | `{{date, EEEE, do MMMM yyyy}}` |
| `MomentJsFormatter` | `DateTime` and `DateTimeOffset` with Moment.js tokens mapped to .NET patterns | `{{date, dddd, MMMM Do}}` |
| `LowercaseFormatter` | `lowercase` with the culture of the language | `{{value, lowercase}}` |
| `UppercaseFormatter` | `uppercase` with the culture of the language | `{{value, uppercase}}` |

```csharp
interpolator.Formatters.Add(new DateFnsFormatter { WeekStartsOn = 1 });
```

```json
{
    "dueDate": "Due {{date, PPPP}}",
    "week": "Week {{date, wo}} of {{date, yyyy}}"
}
```

`DateFnsFormatter` produces the same output as date-fns 4 for every bundled locale, including ordinals (`do`), the long
localized formats (`P`, `PP`, `PPpp`, ...) and week numbering. `WeekStartsOn` and `FirstWeekContainsDate` override the
locale defaults.

### Post processors

Post processors run after interpolation when their keyword is listed in the `postProcess` argument
(`postProcess = "sprintf"` or `postProcess = new[] { "interval", "pseudo" }`).

| Post processor | Keyword | Description |
|---|---|---|
| `SprintfPostProcessor` | `sprintf` | Replaces `%s`, `%d`, ... placeholders in order with the values of the `sprintf` array |
| `IntervalPostProcessor` | `interval` | Picks an interval like `(1){one};(2-7){a few};(8-inf){a lot};` by `count` |
| `PseudoLocalizationPostProcessor` | `pseudo` | Replaces letters with accented look-alikes to find hard-coded or truncated texts |

`PseudoLocalizationOptions` configures the pseudo localization: `LanguagesToPseudo` limits it to some languages,
`LetterMultiplier` and `RepeatedLetters` lengthen vowels to simulate longer translations, `Letters` maps the characters and
`WrapStrings` adds brackets around the text.

### Plural resolver

`DefaultPluralResolver` implements the CLDR cardinal and ordinal rules of all languages. `JsonFormatVersion` selects the
key suffixes (`Version1` to `Version4`, see [Plurals](#plurals)), `UseSimplePluralSuffixIfPossible` controls the
`_plural` suffix of the older formats.

### Language detectors

| Detector | Description |
|---|---|
| `DefaultLanguageDetector` | Always returns the configured language |
| `ThreadLanguageDetector` | Returns `CultureInfo.CurrentCulture` of the current thread, or `FallbackLanguage` |

With `IntegrateToAspNetCore` the request culture is detected through the current thread culture, so it works together
with `UseRequestLocalization`.

### Loggers

| Logger | Package | Description |
|---|---|---|
| `TraceLogger` | `I18Next.Net` | Writes to `System.Diagnostics.Trace`, filtered by `LogLevel` |
| `DefaultExtensionsLogger` | `I18Next.Net.Extensions` | Forwards to `Microsoft.Extensions.Logging`, registered automatically with dependency injection |
| `I18NextSerilogLogger` | `I18Next.Net.Serilog` | Forwards to Serilog |

### Missing key handlers

There is no built-in handler. Subscribe to `DefaultTranslator.MissingKey` or add an `IMissingKeyHandler` to
`MissingKeyHandlers` to log, collect or save missing keys.

## Typed keys with the source generator

`I18Next.Net.Generators` reads the JSON files of the source language at compile time and generates key constants and
typed translation methods, so typos in keys or missing arguments become compile errors. Reference the package, add the
translation files as `AdditionalFiles` and mark a partial class:

```xml
<ItemGroup>
    <PackageReference Include="I18Next.Net.Generators" Version="1.0.0" PrivateAssets="all" />
    <AdditionalFiles Include="locales\**\*.json" />
</ItemGroup>
```

```csharp
[I18NextResources("locales", SourceLanguage = "en")]
public static partial class L;
```

```json
// locales/en/translation.json
{
    "welcome": "Hello {{name}}!",
    "item_one": "{{count}} item",
    "item_other": "{{count}} items",
    "place_ordinal_one": "{{count}}st place",
    "place_ordinal_two": "{{count}}nd place",
    "place_ordinal_few": "{{count}}rd place",
    "place_ordinal_other": "{{count}}th place",
    "friend": "A friend",
    "friend_male": "A boyfriend",
    "menu": {
        "title": "Menu of {{user.name}}",
        "items": [ "Home", "About" ]
    }
}
```

```csharp
i18n.Translation().Welcome(name: "Jane");                 // Hello Jane!
i18n.Translation().Welcome("Jane", language: "de");       // Hallo Jane!
i18n.Translation().Item(count: 5);                        // 5 items
i18n.Translation().Place(22);                             // 22nd place, ordinal = true is passed automatically
i18n.Translation().Friend(context: "male");               // A boyfriend
i18n.Translation().Menu.Title(user: new { name = "Jane" });
i18n.Translation().Menu.Items();                          // string[] { "Home", "About" }
i18n.Translation().Menu.ToObject();                       // the whole group, like TObject
i18n.Translation().Menu.To<MenuModel>();                  // the whole group mapped to a class
await i18n.Translation().WelcomeAsync("Jane");            // every method has an async variant
i18n.Common().Save();                                     // one accessor per namespace

i18n.T(L.Keys.Translation.Menu.Title);                    // "translation:menu.title"
```

Placeholders become parameters, plural keys get a `count` parameter, context variants an optional `context` parameter
and arrays return `string[]`. The XML documentation of every method shows the source text. When the class is not
`static` or is nested, the accessors are static methods (`L.Translation(i18n)`) instead of extension methods.

| Attribute property | Default | Description |
|---|---|---|
| `Path` | | Folder with the `{language}/{namespace}.json` files, relative to the project |
| `SourceLanguage` | `en` | Language whose keys and placeholders are used |
| `DefaultNamespace` | `translation` | Namespace of keys without prefix, used by the analyzer |
| `NamespaceSeparator` | `:` | Separator used in the generated keys |
| `JsonFormatVersion` | `4` | Plural key format of the files (1 to 4) |

The package also checks the translation files and the code using them:

| Rule | Severity | Description |
|---|---|---|
| `I18N001` | Error | A translation file contains invalid JSON (with line and column) |
| `I18N002` | Warning | A key of the source language is missing in another language |
| `I18N003` | Warning | A translation uses placeholders the source language does not use |
| `I18N004` | Warning | A namespace of the source language is missing in another language |
| `I18N005` | Warning | No files of the source language were found |
| `I18N010` | Warning | A string literal passed to `T`, `Ta`, `TObject` or `Exists` is not a key of the source language |

The severities can be changed in `.editorconfig`, e.g. `dotnet_diagnostic.I18N002.severity = suggestion`.

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
| `skipOnVariables`, `alwaysFormat` | ✅ | `DefaultInterpolator.SkipOnVariables` (default `true`), `AlwaysFormat` |
| Formatting with format strings | ✅ | .NET format strings and MomentJS tokens |
| Built-in `number`, `currency`, `datetime` | ✅ | `datetime` matches `Intl.DateTimeFormat` incl. component options |
| Built-in `relativetime`, `list` | ✅ | Bundled CLDR 47 data |
| date-fns formatting | ✅ | `DateFnsFormatter` with the date-fns 4 locales |
| Chained formats | ✅ | |
| Custom formatters | ✅ | `IFormatter` |
| `formatParams` per call | ✅ | Merged into the options of the Intl formats |
| Nesting `$t(key)`, `$t(key, {...})` | ✅ | Custom nesting prefix/suffix |
| Plurals JSON v1, v2, v3 | ✅ | |
| Plurals JSON v4 | ✅ | CLDR categories incl. `_zero` lookup |
| Ordinal plurals | ✅ | `ordinal = true` |
| Plurals with decimal counts | ✅ | `double`, `float` and `decimal` counts use the CLDR rules like `Intl.PluralRules` |
| Context | ✅ | Including plural combinations |
| `defaultValue` incl. plural variants | ✅ | |
| Multiple fallback keys `t([...])` | ✅ | |
| `exists` | ✅ | |
| `returnObjects` | ✅ | `TObject`, `T<TModel>` |
| `joinArrays` | ✅ | |
| Arrays in resources | ✅ | `key.0` |
| `returnNull`, `returnEmptyString` | ✅ | `DefaultTranslator.ReturnEmptyString`, `null` values fall back like `returnNull: false` |
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
| Backends | ✅ | JSON, YAML, XML, INI, gettext, in-memory, custom `ITranslationBackend` |
| i18next-http-backend | ✅ | `HttpBackend`, `AddHttpBackend` with `IHttpClientFactory` |
| i18next-chained-backend | ✅ | `ChainedBackend` with in-memory caching and expiry |
| i18next-resources-to-backend | ✅ | `FuncBackend` |
| `addResource`, `addResourceBundle`, `hasResourceBundle`, `removeResourceBundle` | ✅ | `InMemoryBackend` |
| `reloadResources` | ✅ | `DefaultTranslator.ClearCache` |
| `getFixedT` | ✅ | `GetFixedT(language, namespace, keyPrefix)` |
| `keyPrefix` | ✅ | `keyPrefix` argument and `GetFixedT` |
| ICU message format | ✅ | `I18Next.Net.ICU` |
| Typed keys (TypeScript `CustomTypeOptions`) | ✅ | `I18Next.Net.Generators` source generator |
| Missing key and placeholder checks (i18next-parser, linters) | ✅ | `I18Next.Net.Generators` analyzers |
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
- `DefaultInterpolator.HandleRegexMatch` and `HandleUnescapeRegexMatch` were removed, interpolation runs in a single pass.
  Override `GetValueForExpression` or `EscapeValue` to customize values.
- Values inserted by interpolation are not interpolated or nested again (`SkipOnVariables`, like i18next).
- `CompositeBackend` is obsolete, use `ChainedBackend`.
- The missing key event is raised once per language, fallback languages equal to the requested language are skipped.
- `uppercase` and `lowercase` are handled by the `DefaultFormatter` without registering a formatter.

## Performance

Both libraries translate the same resources (`tests/I18Next.Net.Benchmarks/locales`) with the same scenarios. The .NET
numbers are the mean of BenchmarkDotNet 0.15.8, the Node.js numbers the median of a batched timing loop after warm-up
(`tests/I18Next.Net.Benchmarks/node`). Measured on an Intel Xeon 2.10 GHz with 4 cores, Ubuntu 24.04.

| Scenario | i18next 26.4.2 on Node 22 | I18Next.Net on .NET 8 | I18Next.Net on .NET 10 | Allocated on .NET 10 | Faster than i18next |
|---|---:|---:|---:|---:|---:|
| Simple key `t("simple")` | 3,244 ns | 221 ns | 141 ns | 112 B | 23× |
| Nested key `t("deep.nested.key")` | 2,862 ns | 218 ns | 141 ns | 112 B | 20× |
| Interpolation | 3,447 ns | 447 ns | 328 ns | 352 B | 11× |
| Three interpolations | 4,133 ns | 642 ns | 447 ns | 424 B | 9× |
| Plural | 4,888 ns | 580 ns | 420 ns | 536 B | 12× |
| Context and plural | 5,100 ns | 698 ns | 494 ns | 656 B | 10× |
| Nesting `$t(...)` | 6,553 ns | 1,035 ns | 750 ns | 1240 B | 9× |
| `number` format | 5,505 ns | 819 ns | 600 ns | 464 B | 9× |
| Fallback language | 4,167 ns | 462 ns | 306 ns | 328 B | 14× |
| Other namespace | 2,949 ns | 249 ns | 160 ns | 184 B | 18× |
| `returnObjects` / `TObject` | 17,541 ns | 1,551 ns | 1,136 ns | 2120 B | 15× |
| Missing key | 4,664 ns | 370 ns | 273 ns | 352 B | 17× |

Loaded namespaces are resolved synchronously, the arguments of anonymous objects are read with compiled getters,
interpolation runs in a single pass and parsed formats are cached.

```
dotnet run -c Release --project tests/I18Next.Net.Benchmarks -- --filter '*'

cd tests/I18Next.Net.Benchmarks/node
npm install
npm run bench
```

## Samples

| Sample | Shows |
|---|---|
| [`Example.Features`](samples/Example.Features) | Interpolation, nesting, plurals, ordinals, context, objects, Intl and date-fns formats, fixed translators, fallbacks and post processors in English and German |
| [`Example.Backends`](samples/Example.Backends) | YAML files, embedded resources and objects through the `FuncBackend`, the `HttpBackend` and a cached `ChainedBackend` |
| [`Example.SourceGenerator`](samples/Example.SourceGenerator) | Typed keys and translation methods generated from the JSON files |
| [`Example.MinimalApi`](samples/Example.MinimalApi) | ASP.NET Core minimal API with request localization, `II18Next` and `IStringLocalizer` |
| [`Example.WebApp`](samples/Example.WebApp) | ASP.NET Core MVC with view localization |
| [`Example.ConsoleApp.NetCore`](samples/Example.ConsoleApp.NetCore) | Console application with and without dependency injection |
| [`Example.ConsoleApp.NetFramework`](samples/Example.ConsoleApp.NetFramework) | .NET Framework 4.6.2 console application |

```
dotnet run --project samples/Example.Features
```

## Development

```
dotnet build I18Next.Net.slnx
dotnet test tests/I18Next.Net.Tests
dotnet run -c Release --project tests/I18Next.Net.Benchmarks
```

The tests use xUnit, Shouldly and NSubstitute. The code style follows the default .NET rules in `.editorconfig`
(`dotnet format`), and the CI fails on NuGet packages with known vulnerabilities.

The CLDR data for relative times, lists and the plural rule tests is generated from the official CLDR JSON packages:

```
python3 tools/cldr/generate.py
```

### Releasing

Pushing a version tag builds, tests and packs all packages with that version, publishes them to nuget.org with NuGet
trusted publishing and creates a GitHub release with the packages:

```
git tag v2.0.0
git push origin v2.0.0
```

Tags with a suffix like `v2.0.0-beta.1` are published as prereleases.

## License

[Apache License 2.0](LICENSE)
