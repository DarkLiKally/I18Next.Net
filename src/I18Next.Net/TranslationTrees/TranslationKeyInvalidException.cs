using System;

namespace I18Next.Net.TranslationTrees;

public class TranslationKeyInvalidException : Exception
{
    public TranslationKeyInvalidException(string key)
        : base($"The key `{key}` does not lead to any results in the underlying translation tree.")
    {
        Key = key;
    }

    public TranslationKeyInvalidException(string key, string message)
        : base(message)
    {
        Key = key;
    }

    public TranslationKeyInvalidException(string key, string message, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }

    public string Key { get; }
}