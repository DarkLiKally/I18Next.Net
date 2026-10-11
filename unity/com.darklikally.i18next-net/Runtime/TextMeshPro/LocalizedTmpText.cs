using TMPro;

using UnityEngine;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     Shows the translation of a key in the TextMesh Pro text (<see cref="TMP_Text" />) of the game object.
    /// </summary>
    [AddComponentMenu("I18Next/Localized Text (TextMesh Pro)")]
    [DisallowMultipleComponent]
    public class LocalizedTmpText : LocalizedTextBase
    {
        private TMP_Text _text;

        protected override void SetText(string text)
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();

            if (_text != null)
                _text.text = text;
        }
    }
}
