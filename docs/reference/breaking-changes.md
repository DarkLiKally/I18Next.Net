# Breaking changes

Compared to version 1.0.0:

- `ITranslator` and `II18Next` have new members (`ExistsAsync`, `TranslateObjectAsync`, `TObject`, `T<TModel>`, ...).
  Custom implementations have to add them.
- `Newtonsoft.Json` was replaced by `System.Text.Json`.
- `TranslationTree.GetAllValues()` returns the full key paths (`look.deep` instead of `deep`).
- Translating a key which leads to an object returns i18next's
  `key '...' returned an object instead of string.` message instead of throwing an exception.
- JSON v1 plural suffixes for numbers use `_plural_N` like i18next and negative counts use the absolute value.
- `TraceLogger` respects its `LogLevel`.
- .NET Standard 2.1 and .NET 5 are no longer separate targets, they use the .NET Standard 2.0 build.
