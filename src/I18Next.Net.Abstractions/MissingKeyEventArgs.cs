using System;

namespace I18Next.Net;

/// <summary>
///     The data of the <see cref="Plugins.ITranslator.MissingKey" /> event and of
///     <see cref="Plugins.IMissingKeyHandler.HandleMissingKeyAsync" />.
/// </summary>
/// <param name="language">The language the key is missing in.</param>
/// <param name="ns">The namespace the key is missing in.</param>
/// <param name="key">The missing key without namespace.</param>
/// <param name="possibleKeys">All keys looked up, including the plural and context variants.</param>
public class MissingKeyEventArgs(string language, string ns, string key, string[] possibleKeys) : EventArgs
{
    /// <summary>
    ///     The default value passed with the <c>defaultValue</c> option, if any.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    ///     The missing key without namespace, including the key prefix if one was given.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    ///     The language the key is missing in. With fallback languages, the event is raised for each language the key is
    ///     missing in.
    /// </summary>
    public string Language { get; } = language;

    /// <summary>
    ///     The namespace the key is missing in.
    /// </summary>
    public string Namespace { get; } = ns;

    /// <summary>
    ///     All keys looked up in the order they were added, e.g. <c>item</c>, <c>item_other</c>, <c>item_male</c> and
    ///     <c>item_male_other</c> for a request with <c>count</c> and <c>context</c>.
    /// </summary>
    public string[] PossibleKeys { get; } = possibleKeys;
}
