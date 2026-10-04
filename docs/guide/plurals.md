# Plurals

Plurals use the `count` argument. The JSON format version of the plural resolver decides about the suffixes.
`Version4` is the format of current i18next versions and uses the CLDR plural categories, `Version3` is the default for
compatibility with existing translation files.

```json
{
    "item_zero": "No items",
    "item_one": "{{count}} item",
    "item_other": "{{count}} items",
    "place_ordinal_one": "{{count}}st place",
    "place_ordinal_two": "{{count}}nd place",
    "place_ordinal_few": "{{count}}rd place",
    "place_ordinal_other": "{{count}}th place"
}
```

```csharp
i18n.T("item", new { count = 0 });                    // No items
i18n.T("item", new { count = 1 });                    // 1 item
i18n.T("item", new { count = 5 });                    // 5 items
i18n.T("place", new { count = 22, ordinal = true });  // 22nd place
```

| Format | Example keys |
|---|---|
| `Version1` | `key`, `key_plural`, `key_plural_2`, `key_plural_5` |
| `Version2` | `key`, `key_plural`, `key_1`, `key_2`, `key_5` |
| `Version3` | `key`, `key_plural`, `key_0`, `key_1`, `key_2` |
| `Version4` | `key_zero`, `key_one`, `key_two`, `key_few`, `key_many`, `key_other`, `key_ordinal_one` |

The rules of all 224 CLDR languages for cardinals and 108 for ordinals are included and verified against the CLDR
samples.

## Plural resolver

`DefaultPluralResolver` implements the CLDR cardinal and ordinal rules of all languages. `JsonFormatVersion` selects the
key suffixes (`Version1` to `Version4`, see [Plurals](/guide/plurals)), `UseSimplePluralSuffixIfPossible` controls the
`_plural` suffix of the older formats.
