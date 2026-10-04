using System;

namespace I18Next.Net;

public class MissingKeyEventArgs(string language, string ns, string key, string[] possibleKeys) : EventArgs
{
    public string Key { get; } = key;

    public string Language { get; } = language;

    public string Namespace { get; } = ns;

    public string[] PossibleKeys { get; } = possibleKeys;
}
