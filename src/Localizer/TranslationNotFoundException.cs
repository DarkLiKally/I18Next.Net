using System;

namespace I18Next.Net;

public class TranslationNotFoundException : Exception
{
    public string AlternateString { get; }

    public TranslationNotFoundException(string alternateString)
    {
        AlternateString = alternateString;
    }
}
