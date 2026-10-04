# Translating

The examples use the JSON v4 plural format of current i18next versions.

## Languages

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

## Interpolation

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

## Nesting

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

## Context

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

## Default values and multiple keys

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

## Objects and arrays

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

## Fixed translators and key prefixes

```csharp
var t = i18n.GetFixedT("de", "common", keyPrefix: "menu");
t.T("save");                                              // common:menu.save in German
t.T("translation:title");                                 // other namespaces still work

i18n.T("title", new { keyPrefix = "menu" });              // menu.title
```

## Fallbacks

```csharp
i18n.SetFallbackLanguages("en");                // for every language
i18n.SetLanguageFallbacks("de-CH", "fr", "en"); // only for de-CH, takes precedence
i18n.SetLanguageFallbacks("pt", "es");          // for pt and all regions of it, e.g. pt-BR
i18n.SetFallbackNamespaces("common");
```

Fallback namespaces are checked first in the requested language, then the fallback languages are checked with the
requested and the fallback namespaces.
