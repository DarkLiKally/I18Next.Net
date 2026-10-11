# Changelog

## [2.1.0]

### Added

- `I18NextUnity` holding the I18Next instance, with a default instance loading the translations from `Resources` and
  detecting the language from `Application.systemLanguage`.
- `LocalizedText` for uGUI and `LocalizedTmpText` for TextMesh Pro, translating again when the language or the
  translations change.
- `UnityLogger` writing to the Unity console.
- `link.xml` preserving the I18Next.Net assemblies in IL2CPP builds.
