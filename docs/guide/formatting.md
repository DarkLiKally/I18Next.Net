# Formatting

The i18next built-in formats are available without any registration. They use the culture of the target language and
the bundled [CLDR](https://cldr.unicode.org/) 47 data, so the results match the browser `Intl` APIs used by i18next.

```json
{
    "price": "Price: {{value, currency(EUR)}}",
    "amount": "{{value, number(minimumFractionDigits: 2)}}",
    "date": "{{value, datetime(dateStyle: long; timeStyle: short)}}",
    "updated": "Updated {{value, relativetime(numeric: auto)}}",
    "inQuarters": "{{value, relativetime(quarter)}}",
    "people": "{{value, list}}",
    "choice": "{{value, list(type: disjunction)}}",
    "shout": "{{value, list, uppercase}}"
}
```

```csharp
i18n.T("price", new { value = 1234.5 });                  // Price: €1,234.50
i18n.T("de", "price", new { value = 1234.5 });            // Price: 1.234,50 €
i18n.T("amount", new { value = 5 });                      // 5.00
i18n.T("updated", new { value = -1 });                    // Updated yesterday
i18n.T("inQuarters", new { value = 2 });                  // in 2 quarters
i18n.T("people", new { value = new[] { "Anna", "Ben", "Carl" } });   // Anna, Ben, and Carl
i18n.T("choice", new { value = new[] { "tea", "coffee" } });         // tea or coffee
```

| Format | Options |
|---|---|
| `number` | `minimumFractionDigits`, `maximumFractionDigits`, `useGrouping` |
| `currency(EUR)` | currency code (positional or `currency`), `minimumFractionDigits`, `maximumFractionDigits` |
| `datetime` | `dateStyle`, `timeStyle` (`full`, `long`, `medium`, `short`), `weekday`, `era`, `year`, `month`, `day`, `hour`, `minute`, `second`, `fractionalSecondDigits`, `timeZoneName`, `hour12`, `hourCycle` |
| `relativetime(day)` | unit (positional or `range`): `year`, `quarter`, `month`, `week`, `day`, `hour`, `minute`, `second`; `numeric` (`always`, `auto`); `style` (`long`, `short`, `narrow`) |
| `list` | `type` (`conjunction`, `disjunction`, `unit`), `style` (`long`, `short`, `narrow`) |

Formats are chained with the format separator (`{{value, number, uppercase}}`). Besides the i18next formats every
.NET format string works (`{{value, N2}}`, `{{value, #,##0.00}}`), and date-fns or Moment.js patterns are supported by
the [bundled formatters](/guide/formatting#formatters). Custom formatters implement `IFormatter`:

```csharp
public class ReverseFormatter : IFormatter
{
    public bool CanFormat(object value, string format, string language) => format == "reverse";

    public string Format(object value, string format, string language) => new string(value.ToString().Reverse().ToArray());
}

interpolator.Formatters.Add(new ReverseFormatter());
interpolator.ChainableFormats.Add("reverse"); // allows {{value, reverse, uppercase}}
```

## Format parameters

```json
{
    "price": "{{value, number}}"
}
```

```csharp
i18n.T("price", new { value = 5, formatParams = new { value = new { minimumFractionDigits = 2 } } });   // 5.00
```

## Interpolation options

```csharp
translator.ReturnEmptyString = false;   // empty strings fall back to other languages and default values
interpolator.SkipOnVariables = true;    // default: values containing {{...}} or $t(...) are not processed again
interpolator.AlwaysFormat = true;       // values without a format are passed to the formatters, too
```

## Formatters

Formatters are added to `DefaultInterpolator.Formatters` (or with `AddFormatter` when using dependency injection). The
first formatter whose `CanFormat` returns `true` formats the value, the `DefaultFormatter` is used when none matches.

| Formatter | Formats | Example |
|---|---|---|
| `DefaultFormatter` | Always active. The i18next formats via `IntlFormatter`, otherwise .NET format strings with the culture of the language | `{{value, N2}}` |
| `IntlFormatter` | `number`, `currency`, `datetime`, `relativetime` and `list` like the browser `Intl` APIs | `{{value, currency(EUR)}}` |
| `DateFnsFormatter` | `DateTime` and `DateTimeOffset` with date-fns `format` tokens, all date-fns locales bundled | `{{date, EEEE, do MMMM yyyy}}` |
| `MomentJsFormatter` | `DateTime` and `DateTimeOffset` with Moment.js tokens mapped to .NET patterns | `{{date, dddd, MMMM Do}}` |
| `LowercaseFormatter` | `lowercase` with the culture of the language | `{{value, lowercase}}` |
| `UppercaseFormatter` | `uppercase` with the culture of the language | `{{value, uppercase}}` |

```csharp
interpolator.Formatters.Add(new DateFnsFormatter { WeekStartsOn = 1 });
```

```json
{
    "dueDate": "Due {{date, PPPP}}",
    "week": "Week {{date, wo}} of {{date, yyyy}}"
}
```

`DateFnsFormatter` produces the same output as date-fns 4 for every bundled locale, including ordinals (`do`), the long
localized formats (`P`, `PP`, `PPpp`, ...) and week numbering. `WeekStartsOn` and `FirstWeekContainsDate` override the
locale defaults.
