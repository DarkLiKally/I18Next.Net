using System;

namespace I18Next.Net;

/// <summary>
///     The data of the <see cref="II18Next.LanguageChanged" /> event.
/// </summary>
/// <param name="oldLang">The language used before the change.</param>
/// <param name="newLang">The language used from now on.</param>
public class LanguageChangedEventArgs(string oldLang, string newLang) : EventArgs
{
    /// <summary>
    ///     The language used from now on.
    /// </summary>
    public string NewLanguage { get; } = newLang;

    /// <summary>
    ///     The language used before the change.
    /// </summary>
    public string OldLanguage { get; } = oldLang;
}
