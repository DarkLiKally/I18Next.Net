# Basic sample

Translates texts with the default instance of `I18NextUnity`, which loads
`Resources/locales/{lng}/translation.json` and uses the language of the operating system.

1. Create an empty game object in a scene and add the `BasicSample` component.
2. Enter play mode and switch between English and German with the buttons.

To translate a uGUI text, add the **I18Next > Localized Text** component to a game object with a `Text` and set
the key to `title`. For TextMesh Pro use **I18Next > Localized Text (TextMesh Pro)**. The texts update when the
language changes.
