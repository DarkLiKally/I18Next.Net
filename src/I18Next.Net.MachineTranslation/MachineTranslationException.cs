using System;
using System.Net;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Thrown when a machine translation service rejects a request or returns an unexpected response.
/// </summary>
public class MachineTranslationException : Exception
{
    /// <summary>
    ///     Creates an exception with the given message.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    public MachineTranslationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Creates an exception with the given message caused by another exception.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    /// <param name="innerException">The exception causing this exception.</param>
    public MachineTranslationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Creates an exception for a request the service rejected with the given status code.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    /// <param name="statusCode">The HTTP status code returned by the service.</param>
    public MachineTranslationException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    ///     The HTTP status code returned by the service, if any.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
}
