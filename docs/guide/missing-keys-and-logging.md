# Missing keys and logging

## Missing keys

```csharp
translator.MissingKey += (sender, e) => Console.WriteLine($"{e.Language} {e.Namespace}:{e.Key} ({string.Join(", ", e.PossibleKeys)})");
translator.MissingKeyHandlers.Add(new MyMissingKeyHandler());   // IMissingKeyHandler, e.g. to report missing keys
```

## Missing key handlers

Subscribe to `DefaultTranslator.MissingKey` or add an `IMissingKeyHandler` to `MissingKeyHandlers` to log, collect or
save missing keys. The event arguments contain the language, namespace, key, the keys tried for plurals and context and
the `defaultValue` passed with the translation, if any.

| Handler | Package | Description |
|---|---|---|
| `FileMissingKeyHandler` | `I18Next.Net` | Writes missing keys into `locales/{{lng}}/{{ns}}.missing.json` like the `saveMissing` option of the i18next-fs-backend |
| `HttpMissingKeyHandler` | `I18Next.Net` | Posts missing keys to `locales/add/{{lng}}/{{ns}}` like the `saveMissing` option of the i18next-http-backend |
| `MetricsMissingKeyHandler` | `I18Next.Net.Extensions` | Counts missing keys with the `i18next.missing_keys` counter of the `I18Next.Net` meter |

Every key is written or sent once per language and namespace. The value is the default value or the key itself. Nested
keys are written as nested objects; set `KeySeparator = null` to write them flat.

```csharp
translator.MissingKeyHandlers.Add(new FileMissingKeyHandler("locales/{{lng}}/{{ns}}.missing.json"));
translator.MissingKeyHandlers.Add(new HttpMissingKeyHandler(httpClient, "locales/add/{{lng}}/{{ns}}"));
```

With dependency injection:

```csharp
services.AddI18NextLocalization(i18n => i18n
    .SaveMissingKeysToFiles()                         // development
    .SaveMissingKeysOverHttp("https://translations.example.com/add/{{lng}}/{{ns}}")
    .AddMissingKeyMetrics());                         // e.g. with OpenTelemetry: metrics.AddMeter("I18Next.Net")
```

The counter has the tags `i18next.language` and `i18next.namespace`. `AddMissingKeyMetrics(includeKey: true)` adds the
key as `i18next.key`, which creates a time series per key and should only be used with a limited number of keys.

The ASP.NET Core package receives missing keys reported by i18next in the browser with `MapI18NextMissingKeys`, see
[ASP.NET Core](/integrations/aspnetcore).

## Logging

The default `TraceLogger` writes warnings and errors to `System.Diagnostics.Trace`. With dependency injection an
existing `Microsoft.Extensions.Logging.ILogger` is used automatically. Serilog is supported by `I18NextSerilogLogger`.

```csharp
var logger = new TraceLogger { LogLevel = LogLevel.Debug };
var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver(), new DefaultInterpolator(logger));
```

## Loggers

| Logger | Package | Description |
|---|---|---|
| `TraceLogger` | `I18Next.Net` | Writes to `System.Diagnostics.Trace`, filtered by `LogLevel` |
| `DefaultExtensionsLogger` | `I18Next.Net.Extensions` | Forwards to `Microsoft.Extensions.Logging`, registered automatically with dependency injection |
| `I18NextSerilogLogger` | `I18Next.Net.Serilog` | Forwards to Serilog |
