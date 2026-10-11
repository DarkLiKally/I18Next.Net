using Godot;

namespace I18Next.Net.Godot;

public static class GodotObjectExtensions
{
    /// <summary>
    ///     Translates the given key into the current language with the instance registered by
    ///     <see cref="I18NextGodot.Register" />, e.g. <c>this.T("greeting", new { name })</c>. The language follows the Godot
    ///     locale, also in <c>NOTIFICATION_TRANSLATION_CHANGED</c> handlers notified before the language was changed.
    /// </summary>
    /// <param name="godotObject">The object to translate for, e.g. a node.</param>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key, e.g. count or context.</param>
    /// <returns>Translation value or the key if no instance is registered.</returns>
    public static string T(this GodotObject godotObject, string key, object args = null)
    {
        var i18Next = I18NextGodot.Instance;

        if (i18Next == null)
            return key;

        I18NextGodot.SyncLanguage();

        return i18Next.T(key, args);
    }
}
