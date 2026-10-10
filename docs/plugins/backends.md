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
| `DistributedCacheBackend` | `I18Next.Net.Extensions` | Namespaces cached as JSON in an `IDistributedCache`, e.g. Redis shared by several servers |
| `EntityFrameworkBackend<TContext>` | `I18Next.Net.EntityFrameworkCore` | A database table through Entity Framework Core, editable at runtime |

`InMemoryBackend`, `DistributedCacheBackend` and `EntityFrameworkBackend<TContext>` are writable
(`IWritableTranslationBackend`), so a `ChainedBackend` can store namespaces in them.

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

### Saving to earlier backends

Like the i18next-chained-backend, a namespace provided by a later backend can be saved into the earlier backends
implementing `IWritableTranslationBackend`, e.g. a cache in front of an HTTP backend:

```csharp
var backend = new ChainedBackend(
    new DistributedCacheBackend(distributedCache),
    new HttpBackend(httpClient))
{
    SaveToEarlierBackends = true
};
```

`SaveToEarlierBackends` is disabled by default: once a namespace is saved, the earlier backend provides it, so changes
of the later backends (including `CacheExpiration` reloads and change notifications) are only picked up when the saved
namespace expires in the earlier backend. Namespaces are saved under the requested language, e.g. `de-AT` even if the
later backend fell back to `de`. Exceptions thrown while saving are passed on like exceptions thrown while loading.

## DistributedCacheBackend

The `DistributedCacheBackend` of `I18Next.Net.Extensions` stores namespaces as flat JSON objects in an
`IDistributedCache`, so several servers share the namespaces loaded from a slower backend:

```csharp
services.AddStackExchangeRedisCache(options => options.Configuration = "localhost:6379");
services.AddI18NextLocalization(i18n => i18n
    .AddHttpBackend("locales/{{lng}}/{{ns}}.json")
    .UseDistributedCache(cache =>
    {
        cache.KeyPrefix = "myapp:i18next:";
        cache.EntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
    }));
```

`UseDistributedCache` puts the cache in front of the registered backend with a `ChainedBackend` saving into it. Keys are
`{KeyPrefix}{lng}:{ns}` (`i18next:` by default) and namespaces expire after 7 days by default like in the
i18next-localstorage-backend. There is no fallback to the language part, the following backends take care of it. Cache
failures and unreadable entries are logged and treated as missing namespaces, so translations are still loaded when the
cache is unavailable; set `IgnoreCacheFailures = false` to throw instead. `RemoveNamespaceAsync(lng, ns)` drops a cached
namespace after its translations were updated.

## EntityFrameworkBackend

`I18Next.Net.EntityFrameworkCore` loads translations from a table with one row per language, namespace and key, with a
unique index on these three columns. Add the entity to your context and register a context factory, so the backend can be
used as a singleton:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyI18NextTranslations(); // table I18NextTranslations, or ApplyI18NextTranslations("Texts", "i18n")
    }
}

services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));
services.AddI18NextLocalization(i18n => i18n
    .AddEntityFrameworkBackend<AppDbContext>(backend => backend.CacheExpiration = TimeSpan.FromMinutes(5))
    .AddEntityFrameworkMissingKeyHandler<AppDbContext>("en"));
```

Keys use dots for nested groups (`menu.home`) and regional languages fall back to their language part (`de` for `de-AT`)
when they have no translations. The backend is also registered as `EntityFrameworkBackend<AppDbContext>` to change
translations at runtime:

```csharp
await backend.SetValueAsync("de", "translation", "greeting", "Hallo {{name}}");
await backend.RemoveValueAsync("de", "translation", "obsolete");
await backend.SaveNamespaceAsync("de", "translation", tree); // replaces all translations of the namespace
```

Changes made through the backend raise `TranslationsChanged`, so the translators of the application load the namespace
again. Other servers pick up changes after `CacheExpiration`.

`EntityFrameworkMissingKeyHandler<TContext>` adds keys requested in an existing namespace but missing in the database,
like the `saveMissing` option of i18next. The rows have no value, so they are not loaded until someone translates them,
and each key is only added once. Pass a language, e.g. the development language, to add all missing keys for it instead
of the requested language. It is meant for development and staging environments, as every requested key ends up in the
database.

## Translation tree builders

The file, HTTP and delegate backends accept an `ITranslationTreeBuilderFactory`:

| Builder | Description |
|---|---|
| `HierarchicalTranslationTreeBuilder` | Default. Splits keys at dots into groups, supports objects and arrays |
| `FlatTranslationTreeBuilder` | Keeps keys as they are, the equivalent of `keySeparator: false` |

```csharp
var backend = new JsonFileBackend("locales", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
```
