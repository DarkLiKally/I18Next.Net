# I18Next.Net for Unity

Translates Unity games with [I18Next.Net](https://github.com/DarkLiKally/I18Next.Net), a port of
[i18next](https://www.i18next.com/) for .NET. Requires Unity 2021.3 or newer and works with Mono and IL2CPP on all
platforms including WebGL.

## Installation

Install the release package with the I18Next.Net assemblies and their dependencies: download
`com.darklikally.i18next-net-<version>.tgz` from the
[GitHub releases](https://github.com/DarkLiKally/I18Next.Net/releases) and choose **Window > Package Manager > + >
Install package from tarball**.

Or install the scripts from git and the assemblies with [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity):

1. Install the NuGet package `I18Next.Net` with NuGetForUnity.
2. Choose **Window > Package Manager > + > Install package from git URL** and enter
   `https://github.com/DarkLiKally/I18Next.Net.git?path=unity/com.darklikally.i18next-net#v2.1.0`.

## Usage

Put the i18next JSON files into a `Resources` folder, e.g. `Assets/Resources/locales/en/translation.json`:

```json
{
  "title": "My game",
  "apples_one": "{{count}} apple",
  "apples_other": "{{count}} apples"
}
```

Add **I18Next > Localized Text** to a uGUI `Text` or **I18Next > Localized Text (TextMesh Pro)** to a TextMesh Pro
text and set the key. The texts update when the language changes:

```csharp
I18NextUnity.Language = "de";

var title = I18NextUnity.T("title");
var apples = I18NextUnity.T("apples", new Dictionary<string, object> { ["count"] = 3 });
```

The full documentation is available at
[darklikally.github.io/I18Next.Net/integrations/unity](https://darklikally.github.io/I18Next.Net/integrations/unity).
