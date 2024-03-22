using System;

namespace Localizer;

public class TranslationNotFoundException : Exception
{
    public string AlternateString { get; }

    public TranslationNotFoundException(string alternateString)
    {
        AlternateString = alternateString;
    }
}
