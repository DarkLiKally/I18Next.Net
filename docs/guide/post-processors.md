# Post processors

```csharp
translator.PostProcessors.Add(new SprintfPostProcessor());
translator.PostProcessors.Add(new IntervalPostProcessor());

var pseudoOptions = new PseudoLocalizationOptions();
pseudoOptions.LanguagesToPseudo.Add("en");
translator.PostProcessors.Add(new PseudoLocalizationPostProcessor(pseudoOptions));
```

```json
{
    "sprintf": "The first letters are %s, %s and %s",
    "interval": "(1){one item};(2-7){a few items};(8-inf){a lot of items};"
}
```

```csharp
i18n.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "a", "b", "c" } });   // The first letters are a, b and c
i18n.T("interval", new { postProcess = "interval", count = 3 });                          // a few items
i18n.T("welcome", new { postProcess = "pseudo", name = "Jane" });                          // Ḥḛḛḽḽṓṓ Ĵααṇḛḛ!
```

## Built-in post processors

Post processors run after interpolation when their keyword is listed in the `postProcess` argument
(`postProcess = "sprintf"` or `postProcess = new[] { "interval", "pseudo" }`).

| Post processor | Keyword | Description |
|---|---|---|
| `SprintfPostProcessor` | `sprintf` | Replaces `%s`, `%d`, ... placeholders in order with the values of the `sprintf` array |
| `IntervalPostProcessor` | `interval` | Picks an interval like `(1){one};(2-7){a few};(8-inf){a lot};` by `count` |
| `PseudoLocalizationPostProcessor` | `pseudo` | Replaces letters with accented look-alikes to find hard-coded or truncated texts |

`PseudoLocalizationOptions` configures the pseudo localization: `LanguagesToPseudo` limits it to some languages,
`LetterMultiplier` and `RepeatedLetters` lengthen vowels to simulate longer translations, `Letters` maps the characters and
`WrapStrings` adds brackets around the text.
