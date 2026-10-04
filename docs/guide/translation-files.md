# Translation files

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

All shipped backends are listed under [Backends](/plugins/backends). To use a different file layout override `FindFile`:

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

## Keys and namespaces

```csharp
translator.NamespaceSeparator = "::";    // other::key, null disables namespaces in keys
translator.ContextSeparator = "_";

// Keys containing dots, the equivalent of keySeparator: false
var backend = new JsonFileBackend("locales", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
```

## Resources at runtime

```csharp
var backend = new InMemoryBackend();
backend.AddTranslation("en", "translation", "key", "value");
backend.AddTranslations("de", "translation", new Dictionary<string, string> { ["key"] = "Wert" });
backend.HasNamespace("de", "translation");
backend.RemoveNamespace("de", "translation");

translator.ClearCache();                          // reload all namespaces on the next translation
translator.ClearCache("de", "translation");       // reload one namespace
```
