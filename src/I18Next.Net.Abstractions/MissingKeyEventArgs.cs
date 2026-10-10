using System;

namespace I18Next.Net;

public class MissingKeyEventArgs(string language, string ns, string key, string[] possibleKeys) : EventArgs
{
    /// <summary>
    ///     The default value passed with the <c>defaultValue</c> option, if any.
    /// </summary>
    public string DefaultValue { get; set; }

    public string Key { get; } = key;

    public string Language { get; } = language;

    public string Namespace { get; } = ns;

    public string[] PossibleKeys { get; } = possibleKeys;
}
