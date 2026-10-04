# Backends

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

## HttpBackend

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

## FuncBackend

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

## ChainedBackend

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

## Translation tree builders

The file, HTTP and delegate backends accept an `ITranslationTreeBuilderFactory`:

| Builder | Description |
|---|---|
| `HierarchicalTranslationTreeBuilder` | Default. Splits keys at dots into groups, supports objects and arrays |
| `FlatTranslationTreeBuilder` | Keeps keys as they are, the equivalent of `keySeparator: false` |

```csharp
var backend = new JsonFileBackend("locales", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
```
