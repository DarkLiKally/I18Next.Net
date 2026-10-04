# Interpolators

| Interpolator | Package | Description |
|---|---|---|
| `DefaultInterpolator` | `I18Next.Net` | i18next `{{value}}` interpolation, `$t()` nesting, formats, configurable delimiters |
| `HtmlInterpolator` | `I18Next.Net` | Like the default interpolator but HTML encodes values, used by the ASP.NET Core integration |
| `MessageFormatInterpolator` | `I18Next.Net.ICU` | ICU message format (`{count, plural, one {# item} other {# items}}`) |
| `PolyglotInterpolator` | `I18Next.Net.PolyglotJs` | Polyglot.js phrases (`%{name}`, plurals separated by `\|\|\|\|` and `smart_count`) |

```csharp
var translator = new DefaultTranslator(backend, new MessageFormatInterpolator());
```
