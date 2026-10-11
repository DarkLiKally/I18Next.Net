using System;

using Godot;

namespace I18Next.Net.Godot;

/// <summary>
///     A Godot translation which resolves the messages of one language through I18Next. The plural count of <c>TrN()</c>
///     is passed as i18next <c>count</c>, the Godot context as i18next <c>context</c>. Keys without translation are left
///     to the other translations and the fallback locale of the <see cref="TranslationServer" />.
/// </summary>
public partial class I18NextTranslation : Translation
{
    public I18NextTranslation()
    {
    }

    public I18NextTranslation(II18Next i18Next, string language)
    {
        I18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        Locale = I18NextGodot.ToLocale(language);
    }

    /// <summary>
    ///     The instance translating the messages.
    /// </summary>
    public II18Next I18Next { get; }

    /// <summary>
    ///     The i18next language of the translation, e.g. "de-DE" for the Godot locale "de_DE".
    /// </summary>
    public string Language { get; }

    public override StringName _GetMessage(StringName srcMessage, StringName context)
    {
        return I18NextGodot.Translate(I18Next, Language, srcMessage, context, null) ?? string.Empty;
    }

    public override StringName _GetPluralMessage(StringName srcMessage, StringName srcPluralMessage, int n, StringName context)
    {
        return I18NextGodot.Translate(I18Next, Language, srcMessage, context, n) ?? string.Empty;
    }
}
