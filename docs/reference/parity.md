# Feature parity with i18next

| i18next feature | Status | Notes |
|---|:---:|---|
| Basic translation `t(key)` | ✅ | `T`, `Ta` |
| Namespaces `ns:key` | ✅ | Configurable separator |
| Nested keys `a.b.c` | ✅ | `FlatTranslationTreeBuilder` for `keySeparator: false` |
| Interpolation `{{value}}`, `{{obj.prop}}` | ✅ | |
| Unescaped interpolation `{{- value}}` | ✅ | |
| Escaping | ✅ | `HtmlInterpolator` |
| Custom prefix/suffix | ✅ | `Prefix`, `Suffix`, `UnescapePrefix` |
| `skipOnVariables`, `alwaysFormat` | ✅ | `DefaultInterpolator.SkipOnVariables` (default `true`), `AlwaysFormat` |
| Formatting with format strings | ✅ | .NET format strings and MomentJS tokens |
| Built-in `number`, `currency`, `datetime` | ✅ | `datetime` matches `Intl.DateTimeFormat` incl. component options |
| Built-in `relativetime`, `list` | ✅ | Bundled CLDR 47 data |
| date-fns formatting | ✅ | `DateFnsFormatter` with the date-fns 4 locales |
| Chained formats | ✅ | |
| Custom formatters | ✅ | `IFormatter` |
| `formatParams` per call | ✅ | Merged into the options of the Intl formats |
| Nesting `$t(key)`, `$t(key, {...})` | ✅ | Custom nesting prefix/suffix |
| Plurals JSON v1, v2, v3 | ✅ | |
| Plurals JSON v4 | ✅ | CLDR categories incl. `_zero` lookup |
| Ordinal plurals | ✅ | `ordinal = true` |
| Plurals with decimal counts | ✅ | `double`, `float` and `decimal` counts use the CLDR rules like `Intl.PluralRules` |
| Context | ✅ | Including plural combinations |
| `defaultValue` incl. plural variants | ✅ | |
| Multiple fallback keys `t([...])` | ✅ | |
| `exists` | ✅ | |
| `returnObjects` | ✅ | `TObject`, `T<TModel>` |
| `joinArrays` | ✅ | |
| Arrays in resources | ✅ | `key.0` |
| `returnNull`, `returnEmptyString` | ✅ | `DefaultTranslator.ReturnEmptyString`, `null` values fall back like `returnNull: false` |
| Fallback languages | ✅ | |
| Fallback languages per language | ✅ | `SetLanguageFallbacks`, `UseFallbackLanguagesFor` |
| Fallback from region to language (`de-CH` → `de`) | ✅ | Done by the backends |
| Fallback namespaces | ✅ | |
| `cimode` | ✅ | |
| `dir` | ✅ | `Dir()` |
| `changeLanguage`, `languageChanged` event | ✅ | `Language` setter, `LanguageChanged` |
| Language detection | ✅ | `ILanguageDetector`, `ThreadLanguageDetector`, ASP.NET Core request culture |
| Missing key handling | ✅ | `MissingKey` event, `IMissingKeyHandler` |
| `saveMissing` to backend | ❌ | Implement an `IMissingKeyHandler` |
| Post processors | ✅ | sprintf, interval, pseudo localization, custom `IPostProcessor` |
| Backends | ✅ | JSON, YAML, XML, INI, gettext, in-memory, custom `ITranslationBackend` |
| i18next-http-backend | ✅ | `HttpBackend`, `AddHttpBackend` with `IHttpClientFactory` |
| i18next-chained-backend | ✅ | `ChainedBackend` with in-memory caching and expiry |
| i18next-resources-to-backend | ✅ | `FuncBackend` |
| `addResource`, `addResourceBundle`, `hasResourceBundle`, `removeResourceBundle` | ✅ | `InMemoryBackend` |
| `reloadResources` | ✅ | `DefaultTranslator.ClearCache` |
| `getFixedT` | ✅ | `GetFixedT(language, namespace, keyPrefix)` |
| `keyPrefix` | ✅ | `keyPrefix` argument and `GetFixedT` |
| ICU message format | ✅ | `I18Next.Net.ICU` |
| Typed keys (TypeScript `CustomTypeOptions`) | ✅ | `I18Next.Net.Generators` source generator |
| Missing key and placeholder checks (i18next-parser, linters) | ✅ | `I18Next.Net.Generators` analyzers |
| Logging | ✅ | `TraceLogger`, Microsoft.Extensions.Logging, Serilog |
