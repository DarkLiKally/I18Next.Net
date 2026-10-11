# Unity

The Unity package `com.darklikally.i18next-net` translates games with the same i18next JSON files a JavaScript frontend
uses. It loads the translations from `Resources`, detects the language of the operating system and updates uGUI and
TextMesh Pro texts when the language changes. It requires Unity 2021.3 or newer and works with Mono and IL2CPP on all
platforms including WebGL.

## Installation

Unity does not install NuGet packages, so choose one of the following ways. Don't combine them, the I18Next.Net
assemblies would be imported twice.

### Release package

The `com.darklikally.i18next-net-<version>.tgz` of the [GitHub releases](https://github.com/DarkLiKally/I18Next.Net/releases)
contains the scripts, `I18Next.Net.dll`, `I18Next.Net.Abstractions.dll` and the dependencies Unity doesn't provide:
`System.Text.Json`, `System.Text.Encodings.Web`, `Microsoft.Bcl.AsyncInterfaces` and
`System.Runtime.CompilerServices.Unsafe`. `System.Memory`, `System.Buffers`, `System.Numerics.Vectors` and
`System.Threading.Tasks.Extensions` are part of the .NET Standard 2.1 profile of Unity.

Copy the file into the `Packages` folder of your project and add it to `Packages/manifest.json`, or choose
**Window > Package Manager > + > Install package from tarball**:

```json
{
  "dependencies": {
    "com.darklikally.i18next-net": "file:com.darklikally.i18next-net-2.1.0.tgz"
  }
}
```

If another package already contains one of the dependencies, Unity reports *Multiple precompiled assemblies with the same
name*. Add a scripting define symbol in **Project Settings > Player > Other Settings > Scripting Define Symbols** to
remove the bundled copy:

| Symbol | Removes |
|---|---|
| `I18NEXT_NET_EXCLUDE_SYSTEM_TEXT_JSON` | `System.Text.Json`, `System.Text.Encodings.Web` and `Microsoft.Bcl.AsyncInterfaces` |
| `I18NEXT_NET_EXCLUDE_UNSAFE` | `System.Runtime.CompilerServices.Unsafe` |

### Git URL and NuGetForUnity

Install the NuGet package `I18Next.Net` with [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). It imports
the dependencies and skips the assemblies Unity and other packages already contain. Then add the scripts from git with
**Window > Package Manager > + > Install package from git URL** or in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.darklikally.i18next-net": "https://github.com/DarkLiKally/I18Next.Net.git?path=unity/com.darklikally.i18next-net#v2.1.0"
  }
}
```

Keep the versions of the NuGet package and the git tag in sync.

## Setup

Put the translations into a `Resources` folder as `locales/{lng}/{ns}.json`, e.g.
`Assets/Resources/locales/en/translation.json`:

```json
{
  "title": "My game",
  "greeting": "Hello {{name}}!",
  "apples_one": "{{count}} apple",
  "apples_other": "{{count}} apples"
}
```

`I18NextUnity.Instance` creates a default instance on first use. It loads the translations from `Resources`, uses the
language of the operating system with `en` as fallback language, the i18next v4 plural suffixes like `_one` and `_other`
and logs warnings to the Unity console:

```csharp
using System.Collections.Generic;

using I18Next.Net.Unity;

var title = I18NextUnity.T("title");
var apples = I18NextUnity.T("apples", new Dictionary<string, object> { ["count"] = 3 });

I18NextUnity.Language = "de";
```

Set your own instance before the first scene is loaded to change the defaults. `CreateDefault` takes the fallback
language and the folder below `Resources`, and returns an `I18NextNet` you can configure further:

```csharp
using I18Next.Net.Unity;

using UnityEngine;

public static class Localization
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var i18n = I18NextUnity.CreateDefault(fallbackLanguage: "en", resourcesPath: "i18n");
        i18n.FallbackNamespaces = new[] { "common" };
        i18n.Language = PlayerPrefs.GetString("language", i18n.Language);

        I18NextUnity.Instance = i18n;
    }
}
```

Any `II18Next` works, e.g. with your own backend:

```csharp
var logger = new UnityLogger();
var backend = I18NextUnity.CreateResourcesBackend();
var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
    new DefaultInterpolator(logger));

I18NextUnity.Instance = new I18NextNet(backend, translator, new SystemLanguageDetector("en")) { Language = "en" };
```

| API | Description |
|---|---|
| `I18NextUnity.Instance` | The instance translating the components. Setting another instance translates them again |
| `I18NextUnity.Language` | The language of the instance. Setting it translates the components again |
| `I18NextUnity.T(key, args)` | Translates a key |
| `I18NextUnity.LanguageChanged` / `TranslationsChanged` | Raised on the main thread when the language or the translations change |
| `I18NextUnity.CreateDefault(fallbackLanguage, resourcesPath)` | Creates the default instance |
| `I18NextUnity.CreateResourcesBackend(path)` | Loads `Resources/{path}/{lng}/{ns}.json`, works on all platforms |
| `I18NextUnity.CreateStreamingAssetsBackend(path)` | Reads `StreamingAssets/{path}/{lng}/{ns}.json`, not on Android and WebGL |
| `SystemLanguageDetector` | Detects the language from `Application.systemLanguage` |
| `UnityLogger` | Writes to the Unity console, filtered by `LogLevel` |

`SystemLanguageDetector` maps `Application.systemLanguage` to language codes like `de` or `ja`; `ChineseSimplified` and
`ChineseTraditional` become `zh-Hans` and `zh-Hant`, `Norwegian` becomes `nb` and `SerboCroatian` becomes `sr`. Regional
codes fall back to the files of the language, e.g. `zh-Hans` to `locales/zh/`. Unknown languages use the fallback
language.

## Components

Add **I18Next > Localized Text** to a game object with a uGUI `Text` (*Text - Legacy* in Unity 6), or
**I18Next > Localized Text (TextMesh Pro)** to a game object with a `TextMeshProUGUI` or `TextMeshPro` text. The
components translate when they start and again when the language or the translations change.

| Field | Description |
|---|---|
| `Key` | The key, optionally with a namespace like `common:save` |
| `Namespace` | Translates from another namespace than the default namespace |
| `Arguments` | Named values like `name` for `{{name}}`. The value of `count` is converted to a number to resolve plurals |

Set the values that change at runtime from code:

```csharp
var text = GetComponent<LocalizedText>();
text.Args = new Dictionary<string, object> { ["name"] = player.Name };
text.Count = apples;
```

`Args` accepts dictionaries and objects, values like strings and numbers are available as `{{value}}`. `Args` and `Count`
override `Arguments` with the same name. Call `Refresh()` after changing `Arguments`.

`LocalizedText` is compiled when `com.unity.ugui` is installed, `LocalizedTmpText` with `com.unity.textmeshpro` 3.0 or
newer, or `com.unity.ugui` 2.0 or newer, which contains TextMesh Pro in Unity 6. Derive from `LocalizedTextBase` to
translate other texts, e.g. a UI Toolkit label:

```csharp
using I18Next.Net.Unity;

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LocalizedLabel : LocalizedTextBase
{
    [SerializeField]
    private string _labelName;

    protected override void SetText(string text)
    {
        GetComponent<UIDocument>().rootVisualElement.Q<Label>(_labelName).text = text;
    }
}
```

## Threading

Unity APIs are only available on the main thread. `I18NextUnity` raises `LanguageChanged` and `TranslationsChanged` on
the main thread, also when the language or the translations change on another thread, e.g. in a watching backend, so
the components update on the main thread. Translations are loaded when they are first used, and `Resources.Load` only
works on the main thread, so translate on the main thread when using the `Resources` backend.

## Hot reload

Wrap the backend in a [`FileWatchingBackend`](/guide/hot-reload) to see changed translation files in play mode without
restarting it. In the Editor the files in `Assets` can be watched directly:

```csharp
#if UNITY_EDITOR
var path = Path.Combine(Application.dataPath, "Resources", "locales");
var backend = new FileWatchingBackend(new JsonFileBackend(path), path);
#else
var backend = I18NextUnity.CreateResourcesBackend();
#endif
```

## IL2CPP and code stripping

- The package contains a `link.xml` preserving `I18Next.Net` and `I18Next.Net.Abstractions`. Unity ignores `link.xml`
  files in packages, so the package passes it to the linker in the build.
- Arguments given as anonymous objects like `new { name = "Jane" }` are read with reflection. Managed code stripping
  can remove their property getters, so use `Dictionary<string, object>` in IL2CPP builds. The interceptors of the
  [source generator](./source-generator) which replace anonymous objects need C# 12 and don't work in Unity, which uses
  C# 9.
- `T<TModel>` maps translations to models with the reflection based `System.Text.Json` serializer. Preserve the model
  types, e.g. with `[Preserve]`, or use `TObject` and string arrays in IL2CPP builds.

## WebGL

WebGL has no threads and no synchronous file access. The `Resources` backend works as everything is loaded
synchronously on the main thread. `StreamingAssets` and the `HttpBackend` don't work in WebGL; download translations
with `UnityWebRequest` yourself and pass them to an `InMemoryBackend` or `FuncBackend.FromJson` before translating.

## Sample

Import the **Basic** sample in **Window > Package Manager > I18Next.Net > Samples**. Add the `BasicSample` component to
a game object and enter play mode to switch between English and German.
