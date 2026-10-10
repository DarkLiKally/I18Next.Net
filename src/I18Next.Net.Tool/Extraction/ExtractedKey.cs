using System.Collections.Generic;

using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Extraction;

/// <summary>
///     A translation key found in the source code.
/// </summary>
internal sealed class ExtractedKey(string @namespace, string key, string file, int line)
{
    public string Namespace { get; } = @namespace;

    public string Key { get; } = key;

    public string File { get; } = file;

    public int Line { get; } = line;

    /// <summary>
    ///     The call passes a <c>count</c>, so the key needs plural forms.
    /// </summary>
    public bool HasCount { get; set; }

    public bool Ordinal { get; set; }

    /// <summary>
    ///     The context passed as string literal.
    /// </summary>
    public string Context { get; set; }

    /// <summary>
    ///     The call returns the object below the key, e.g. <c>TObject</c>.
    /// </summary>
    public bool ReturnsObject { get; set; }

    /// <summary>
    ///     Gets the keys a language needs for this usage, e.g. <c>item_one</c> and <c>item_other</c> for a count.
    /// </summary>
    public IEnumerable<string> GetKeys(string language)
    {
        var key = Context == null ? Key : Key + PluralForms.Separator + Context;

        return HasCount ? PluralForms.GetKeys(key, language, Ordinal) : [key];
    }
}
