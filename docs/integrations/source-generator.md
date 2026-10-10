# Typed keys with the source generator

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
| `I18N011` | Warning | An anonymous object passed to `T`, `Ta` or an `IStringLocalizer` does not contain a placeholder of the translation |
| `I18N012` | Info | An anonymous object passed to `T`, `Ta` or an `IStringLocalizer` contains a member no translation uses |

The severities can be changed in `.editorconfig`, e.g. `dotnet_diagnostic.I18N002.severity = suggestion`.

`I18N011` and `I18N012` only check anonymous objects like `new { name = "Jane" }`, not dictionaries or variables. They
use the placeholders of the variants the call can select: the context variant when `context` is a constant, the plural
forms when `count` is passed and the ordinal forms when `ordinal = true` is passed. Options like `count`, `context`,
`defaultValue`, `lng` or `formatParams` are not reported as unused, the members of `replace = new { ... }` are checked
instead of the options, and calls with `keyPrefix` or `ns` are skipped. Of nested placeholders like `{{user.name}}`
only `user` is checked, keys nested with `$t(...)` are not followed and unused members are not reported for
translations which nest other keys.

```csharp
i18n.T("welcome", new { nam = "Jane" }); // I18N011: 'welcome' uses the placeholder 'name' which is not passed
                                         // I18N012: the argument 'nam' is not used by the translation 'welcome'
```

## Code fixes

The package also contains code fixes which IDEs offer as quick actions:

| Rule | Code fix |
|---|---|
| `I18N010` | Change the key to one of up to three similar keys, e.g. `welcom` to `welcome`, `item_plural` to `item` or `save` to `common:save` |
| `I18N010` | Add the key to the file of the source language or to the files of all languages, also for all keys of a document, project or solution |
| `I18N002` | Add the missing key, or all missing keys, to the file of the language |
| `I18N011` | Rename a member which is similar to the missing placeholder, e.g. `nam` to `name` |

Added keys are nested like the existing keys (`menu.title` becomes `"menu": { "title": ... }` unless the file uses
dotted keys), placed like in the file of the source language, else alphabetically in sorted objects and else at the end,
with the indentation and line breaks of the file. Unknown keys get their last segment as text (`title` for
`menu.title`), so they are visible in the application until they are translated. Missing keys get the text of the
source language to be translated later, an empty text would be shown as it is because empty strings are valid translations
by default (`ReturnEmptyString`). Keys in arrays are not added.

When the generator is referenced as a project instead of the package, the code fixes need their own reference:

```xml
<ProjectReference Include="..\I18Next.Net.Generators\I18Next.Net.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<ProjectReference Include="..\I18Next.Net.Generators.CodeFixes\I18Next.Net.Generators.CodeFixes.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```
