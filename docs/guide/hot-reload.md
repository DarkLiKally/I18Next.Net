# Hot reload

The `FileWatchingBackend` watches the translation files of a file based backend and loads changed namespaces again
without restarting the application. It works with the JSON, YAML, XML, INI and gettext backends as long as the files are
stored as `{lng}/{ns}.{extension}`. Changes to other files reload all namespaces.

```csharp
var backend = new FileWatchingBackend(new JsonFileBackend("locales"), "locales");
var i18n = new I18NextNet(backend, new DefaultTranslator(backend));
```

With dependency injection the registered backend is wrapped:

```csharp
services.AddI18NextLocalization(i18n =>
{
    i18n.AddBackend(new JsonFileBackend("locales"));

    if (environment.IsDevelopment())
        i18n.WatchTranslationFiles("locales");
});
```

Editors often write a file several times when saving, so changes are collected for `Delay` (200 ms by default) before
they are reported.

## Change notifications

Backends implementing `INotifyingTranslationBackend` raise `TranslationsChanged` with the changed language and
namespace. A changed language without region (e.g. `de`) also affects its regional languages (e.g. `de-AT`); `null`
means all languages or namespaces.

- `DefaultTranslator` drops the affected namespaces from its cache, so the next translation loads them again.
- `ChainedBackend` drops them from its cache and forwards the notification of its backends.
- UI integrations like Blazor and XAML render the affected texts again.

Custom backends, e.g. one reading translations from a database, can implement the interface to update translations at
runtime.
