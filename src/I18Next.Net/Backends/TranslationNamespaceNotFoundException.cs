using System;

namespace I18Next.Net.Backends;

/// <summary>
///     Indicates that a translation namespace does not exist. Can be thrown by custom backends; the built-in backends
///     return null for missing namespaces instead.
/// </summary>
public class TranslationNamespaceNotFoundException : Exception
{
    /// <summary>
    ///     Creates the exception with a default message.
    /// </summary>
    /// <param name="namespace">The missing namespace.</param>
    public TranslationNamespaceNotFoundException(string @namespace)
        : base($"The translation namespace {@namespace} does not exist.")
    {
        Namespace = @namespace;
    }

    /// <summary>
    ///     Creates the exception with the given message.
    /// </summary>
    /// <param name="namespace">The missing namespace.</param>
    /// <param name="message">The message describing the error.</param>
    public TranslationNamespaceNotFoundException(string @namespace, string message)
        : base(message)
    {
        Namespace = @namespace;
    }

    /// <summary>
    ///     Creates the exception with the given message and inner exception.
    /// </summary>
    /// <param name="namespace">The missing namespace.</param>
    /// <param name="message">The message describing the error.</param>
    /// <param name="innerException">The exception causing this exception.</param>
    public TranslationNamespaceNotFoundException(string @namespace, string message, Exception innerException)
        : base(message, innerException)
    {
        Namespace = @namespace;
    }

    /// <summary>
    ///     The missing namespace.
    /// </summary>
    public string Namespace { get; }
}
