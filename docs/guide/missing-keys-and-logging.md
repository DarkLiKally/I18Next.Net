# Missing keys and logging

## Missing keys

```csharp
translator.MissingKey += (sender, e) => Console.WriteLine($"{e.Language} {e.Namespace}:{e.Key} ({string.Join(", ", e.PossibleKeys)})");
translator.MissingKeyHandlers.Add(new MyMissingKeyHandler());   // IMissingKeyHandler, e.g. to report missing keys
```

## Missing key handlers

There is no built-in handler. Subscribe to `DefaultTranslator.MissingKey` or add an `IMissingKeyHandler` to
`MissingKeyHandlers` to log, collect or save missing keys.

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
