# WPF and .NET MAUI

`I18Next.Net.Wpf` and `I18Next.Net.Maui` provide the `T` markup extension. Its text updates when the language changes
and, with a [watching backend](#hot-reload), when the translation files change.

```
dotnet add package I18Next.Net.Wpf
dotnet add package I18Next.Net.Maui
```

```xml
<Window xmlns:i18n="https://github.com/DarkLiKally/I18Next.Net" Title="{i18n:T title}">
    <StackPanel>
        <TextBlock Text="{i18n:T menu.title}" />
        <TextBlock Text="{i18n:T menu.title, Namespace=common}" />
        <TextBlock Text="{i18n:T common:menu.title}" />
        <TextBlock Text="{i18n:T greeting, Args={Binding User}}" />
        <TextBlock Text="{i18n:T inbox, Count={Binding UnreadCount}}" />
        <TextBlock Text="{i18n:T total, Args={Binding Total}}" />
    </StackPanel>
</Window>
```

| Property | Description |
|---|---|
| `Key` | The key, also the first positional argument |
| `Namespace` | Translates from another namespace than the default namespace |
| `Args` | A value or a binding. Strings, numbers, dates and lists are available as `{{value}}`, objects and dictionaries provide their properties, e.g. `{{Name}}` for `Args={Binding User}` |
| `Count` | A value or a binding providing the count for plurals |

The bindings are one way, so the extension also works on two way properties like `TextBox.Text` and in style setters.
Translations are resolved on the UI thread, so prefer local backends like files or embedded resources.

## WPF

Set the instance before the first window is loaded, e.g. in `App.OnStartup`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    var locales = Path.Combine(AppContext.BaseDirectory, "locales");
    var backend = new JsonFileBackend(locales);

    I18NextXaml.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

    base.OnStartup(e);
}
```

With dependency injection, resolve the registered instance:

```csharp
var services = new ServiceCollection()
    .AddI18NextLocalization(i18n => i18n
        .AddBackend(new JsonFileBackend(locales))
        .UseDefaultLanguage("en"))
    .BuildServiceProvider();

I18NextXaml.Instance = services.GetRequiredService<II18Next>();
```

Changing `II18Next.Language` translates all views again. Setting another instance does the same.

## .NET MAUI

`UseI18Next` registers I18Next like [`AddI18NextLocalization`](./dependency-injection) and uses it for the markup
extension:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseI18Next(i18n => i18n
            .AddBackend(FuncBackend.FromStream((lng, ns) =>
                typeof(App).Assembly.GetManifestResourceStream($"Locales.{lng}.{ns}.json")))
            .UseDefaultLanguage("en")
            .UseFallbackLanguage("en"));

    return builder.Build();
}
```

Embedded resources work on all platforms, e.g. `Resources/Locales/en.translation.json`:

```xml
<ItemGroup>
    <EmbeddedResource Include="Resources\Locales\*.json" LogicalName="Locales.%(Filename)%(Extension)" />
</ItemGroup>
```

Inject `II18Next` to switch the language, e.g. from a picker.

## Hot reload

Translations are rendered again when the backend implements `INotifyingTranslationBackend`. During development,
watch the translation files with the `FileWatchingBackend`:

```csharp
var backend = new FileWatchingBackend(new JsonFileBackend(locales), locales);
```

or with dependency injection:

```csharp
i18n.AddBackend(new JsonFileBackend(locales)).WatchTranslationFiles(locales);
```

The watcher reports changes on a background thread, the views are updated on the UI thread.
