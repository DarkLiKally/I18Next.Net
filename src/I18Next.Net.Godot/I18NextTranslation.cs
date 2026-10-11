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
    /// <summary>
    ///     Creates a translation without an instance, which translates nothing. Required by Godot to create instances of
    ///     the type, use <see cref="I18NextTranslation(II18Next, string)" /> instead.
    /// </summary>
    public I18NextTranslation()
    {
    }

    /// <summary>
    ///     Creates a translation of the given language. Its <see cref="Translation.Locale" /> is the Godot locale of the
    ///     language. <see cref="I18NextGodot.Register" /> creates and adds the translations to the
    ///     <see cref="TranslationServer" />.
    /// </summary>
    /// <param name="i18Next">The instance translating the messages.</param>
    /// <param name="language">The i18next language of the translation, e.g. <c>de-DE</c>.</param>
    /// <exception cref="ArgumentNullException">
    ///     If <paramref name="i18Next" /> or <paramref name="language" /> is <c>null</c>.
    /// </exception>
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
    ///     The i18next language of the translation, e.g. <c>de-DE</c> for the Godot locale <c>de_DE</c>.
    /// </summary>
    public string Language { get; }

    /// <summary>
    ///     Translates the message key with I18Next, passing the Godot context as i18next <c>context</c>.
    /// </summary>
    /// <param name="srcMessage">The key to be translated.</param>
    /// <param name="context">The Godot context or an empty name.</param>
    /// <returns>The translation or an empty name if the key has no translation, so Godot tries other translations.</returns>
    public override StringName _GetMessage(StringName srcMessage, StringName context)
    {
        return I18NextGodot.Translate(I18Next, Language, srcMessage, context, null) ?? string.Empty;
    }

    /// <summary>
    ///     Translates the message key with I18Next, passing the plural count as i18next <c>count</c> and the Godot context
    ///     as i18next <c>context</c>. The plural forms come from the i18next plural suffixes of the key, so
    ///     <paramref name="srcPluralMessage" /> is ignored.
    /// </summary>
    /// <param name="srcMessage">The key to be translated.</param>
    /// <param name="srcPluralMessage">The plural message, ignored.</param>
    /// <param name="n">The count used to resolve the plural form.</param>
    /// <param name="context">The Godot context or an empty name.</param>
    /// <returns>The translation or an empty name if the key has no translation, so Godot tries other translations.</returns>
    public override StringName _GetPluralMessage(StringName srcMessage, StringName srcPluralMessage, int n, StringName context)
    {
        return I18NextGodot.Translate(I18Next, Language, srcMessage, context, n) ?? string.Empty;
    }
}
