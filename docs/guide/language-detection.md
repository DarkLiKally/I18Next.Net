# Language detection

```csharp
var i18n = new I18NextNet(backend, translator, new ThreadLanguageDetector())
{
    DetectLanguageOnEachTranslation = true
};

CultureInfo.CurrentCulture = new CultureInfo("de-DE");
i18n.T("welcome");      // translated to de-DE without changing i18n.Language

i18n.UseDetectedLanguage();   // sets i18n.Language to the detected language
```

## Language detectors

| Detector | Description |
|---|---|
| `DefaultLanguageDetector` | Always returns the configured language |
| `ThreadLanguageDetector` | Returns `CultureInfo.CurrentCulture` of the current thread, or `FallbackLanguage` |

With `IntegrateToAspNetCore` the request culture is detected through the current thread culture, so it works together
with `UseRequestLocalization`.
