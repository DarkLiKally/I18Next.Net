using System;

namespace I18Next.Net.Backends;

public class TranslationNamespaceNotFoundException : Exception
{
    public TranslationNamespaceNotFoundException(string @namespace)
        : base($"The translation namespace {@namespace} does not exist.")
    {
        Namespace = @namespace;
    }

    public TranslationNamespaceNotFoundException(string @namespace, string message)
        : base(message)
    {
        Namespace = @namespace;
    }

    public TranslationNamespaceNotFoundException(string @namespace, string message, Exception innerException)
        : base(message, innerException)
    {
        Namespace = @namespace;
    }

    public string Namespace { get; }
}