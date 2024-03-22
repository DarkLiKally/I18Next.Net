using System;

namespace I18Next.Net;

public class LanguageChangedEventArgs : EventArgs
{
    public LanguageChangedEventArgs(string oldLang, string newLang)
    {
        OldLanguage = oldLang;
        NewLanguage = newLang;
    }

    public string NewLanguage { get; }

    public string OldLanguage { get; }
}