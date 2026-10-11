# Godot

`I18Next.Net.Godot` translates Godot 4 C# projects (Godot 4.3 and later) with i18next JSON files. Godot's own
localization is resolved through i18next, so `Tr()`, `TrN()`, GDScript's `tr()` and auto translated controls like
`Label` and `Button` show the i18next translations.

```
dotnet add package I18Next.Net.Godot
```

Register the instance once, e.g. in an [autoload](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html):

```csharp
public partial class Localization : Node
{
    public override void _EnterTree()
    {
        var logger = new TraceLogger();
        var backend = new GodotFileBackend("res://locales");
        var translator = new DefaultTranslator(backend, logger,
            new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 }, new DefaultInterpolator(logger));
        var i18n = new I18NextNet(backend, translator) { Language = "en" };
        i18n.SetFallbackLanguages("en");

        I18NextGodot.Register(i18n, "en", "de");
    }
}
```

`Register` adds an `I18NextTranslation` for each language to the `TranslationServer` and sets the Godot locale to the
language of the instance. Call it on the main thread, registering another instance replaces the previous one and
`I18NextGodot.Unregister()` removes the translations.

## Translating

Use the keys as texts of controls, they are translated when auto translation is enabled (the default):

```
[node name="Title" type="Label" parent="."]
text = "menu.title"
```

```csharp
Tr("menu.title");
Tr("friend", "male");                      // context: friend_male
TrN("inbox", "inbox", 3);                  // count: inbox_one, inbox_other
Tr("common:save");                         // namespace
```

The plural count of `TrN()` becomes the i18next `count`, the context of `Tr()` and `TrN()` the i18next `context`. Keys
without translation are left to the other translations and the fallback locale of the `TranslationServer`, so texts
that are no keys stay unchanged. Every auto translated text is looked up, so disable `auto_translate_mode` on controls
showing user content.

`Tr()` has no arguments for interpolation. Use the `T` extension method on any node or object, also for nesting,
formatting and all other i18next options:

```csharp
_inbox.Text = this.T("inbox", new { count = messages.Count });
_greeting.Text = this.T("greeting", new { name = player.Name });
```

Texts set from code are not translated again when the language changes. Set them again when the node receives
`NotificationTranslationChanged` and disable auto translation for them:

```csharp
public override void _Notification(int what)
{
    if (what == NotificationTranslationChanged)
        UpdateTexts();
}
```

## Changing the language

The Godot locale and the language of the instance are kept in sync: changing one changes the other and translates all
controls again.

```csharp
TranslationServer.SetLocale("de");
I18NextGodot.Instance.Language = "de";
```

Godot writes locales with underscores, `de_DE` becomes the i18next language `de-DE`. `I18NextGodot.ToLocale` and
`I18NextGodot.ToLanguage` convert between both. The `TranslationServer` uses the closest registered language, e.g. the
`de` translation for `de_AT`, and i18next loads `de` files for `de-AT` languages. Start with the language of the
operating system:

```csharp
TranslationServer.SetLocale(OS.GetLocale());
```

The `T` extension method translates into the current Godot locale, also in `NotificationTranslationChanged` handlers
called before the language of the instance was changed. Prefer it over `I18NextGodot.Instance.T` in nodes.

## Translation files

`GodotFileBackend` reads `{lng}/{ns}.json` files from the Godot file system, e.g. `res://locales/en/translation.json`.
`res://` paths work in the editor and in exported projects on all platforms, `user://` paths load translations
downloaded at runtime. Missing regional files fall back to the language, e.g. `de` for `de-AT`.

```
locales/
  en/translation.json
  de/translation.json
```

Godot exports `.json` files with the other resources. When exporting selected resources only, add `locales/*` to the
*Filters to export non-resource files/folders* of the export preset.

## Hot reload

Translations changed through an `INotifyingTranslationBackend` translate all controls again. While running from the
editor, watch the translation files of the project with the [`FileWatchingBackend`](/guide/hot-reload):

```csharp
ITranslationBackend backend = new GodotFileBackend("res://locales");

if (OS.HasFeature("editor"))
    backend = new FileWatchingBackend(backend, ProjectSettings.GlobalizePath("res://locales"));
```

## Native AOT

Godot exports C# projects for iOS with Native AOT. I18Next.Net and `I18Next.Net.Godot` are
[Native AOT compatible](/guide/native-aot). The source generator converts anonymous arguments of `II18Next` calls, but
not of the `T` extension method, so pass dictionaries there or call the instance:

```csharp
this.T("greeting", new Dictionary<string, object> { ["name"] = player.Name });
I18NextGodot.Instance.T("greeting", new { name = player.Name });
```

The [`Example.Godot`](https://github.com/DarkLiKally/I18Next.Net/tree/develop/samples/Example.Godot) sample shows auto
translated controls, plurals and a language switch.
