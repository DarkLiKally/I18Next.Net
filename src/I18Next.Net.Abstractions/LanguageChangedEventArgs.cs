using System;

namespace I18Next.Net;

public class LanguageChangedEventArgs(string oldLang, string newLang) : EventArgs
{
    public string NewLanguage { get; } = newLang;

    public string OldLanguage { get; } = oldLang;
}
