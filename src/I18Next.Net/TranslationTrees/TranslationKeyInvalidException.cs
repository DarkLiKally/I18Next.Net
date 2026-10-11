using System;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Thrown by a <see cref="TranslationTree" /> when a key leads to a group instead of a translation or continues below a
///     translation. The <see cref="Plugins.DefaultTranslator" /> handles it and translates such keys as missing or as
///     groups.
/// </summary>
public class TranslationKeyInvalidException : Exception
{
    /// <summary>
    ///     Creates the exception with a default message.
    /// </summary>
    /// <param name="key">The invalid key.</param>
    public TranslationKeyInvalidException(string key)
        : base($"The key `{key}` does not lead to any results in the underlying translation tree.")
    {
        Key = key;
    }

    /// <summary>
    ///     Creates the exception with the given message.
    /// </summary>
    /// <param name="key">The invalid key.</param>
    /// <param name="message">The message describing why the key is invalid.</param>
    public TranslationKeyInvalidException(string key, string message)
        : base(message)
    {
        Key = key;
    }

    /// <summary>
    ///     Creates the exception with the given message and inner exception.
    /// </summary>
    /// <param name="key">The invalid key.</param>
    /// <param name="message">The message describing why the key is invalid.</param>
    /// <param name="innerException">The exception causing this exception.</param>
    public TranslationKeyInvalidException(string key, string message, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }

    /// <summary>
    ///     The invalid key.
    /// </summary>
    public string Key { get; }
}
