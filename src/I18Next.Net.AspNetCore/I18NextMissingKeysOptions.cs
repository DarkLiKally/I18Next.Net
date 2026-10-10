#if NET6_0_OR_GREATER
namespace I18Next.Net.AspNetCore;

/// <summary>
///     Options of the endpoint which receives the missing keys reported by i18next in the browser.
/// </summary>
public class I18NextMissingKeysOptions : I18NextEndpointOptions
{
    /// <summary>
    ///     The maximum size of a request body in bytes. Larger requests are rejected with status 413.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 64 * 1024;

    /// <summary>
    ///     The maximum number of keys in one request. Requests with more keys are rejected with status 400.
    /// </summary>
    public int MaxKeys { get; set; } = 100;
}
#endif
