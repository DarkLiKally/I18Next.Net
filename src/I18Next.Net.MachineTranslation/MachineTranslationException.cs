using System;
using System.Net;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Thrown when a machine translation service rejects a request or returns an unexpected response.
/// </summary>
public class MachineTranslationException : Exception
{
    public MachineTranslationException(string message)
        : base(message)
    {
    }

    public MachineTranslationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

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
