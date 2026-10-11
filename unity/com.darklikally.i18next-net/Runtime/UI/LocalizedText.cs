using UnityEngine;
using UnityEngine.UI;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     Shows the translation of a key in the <see cref="Text" /> of the game object.
    /// </summary>
    [AddComponentMenu("I18Next/Localized Text")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class LocalizedText : LocalizedTextBase
    {
        private Text _text;

        protected override void SetText(string text)
        {
            if (_text == null)
                _text = GetComponent<Text>();

            if (_text != null)
                _text.text = text;
        }
    }
}
