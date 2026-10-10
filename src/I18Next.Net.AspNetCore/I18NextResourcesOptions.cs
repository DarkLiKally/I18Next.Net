#if NET6_0_OR_GREATER
namespace I18Next.Net.AspNetCore;

/// <summary>
///     Options of the endpoint which serves translations to i18next in the browser.
/// </summary>
public class I18NextResourcesOptions : I18NextEndpointOptions
{
    /// <summary>
    ///     The value of the <c>Cache-Control</c> header or <c>null</c> to send none. Defaults to <c>no-cache</c>, so browsers
    ///     revalidate with the <c>ETag</c> and get changed translations immediately.
    /// </summary>
    public string CacheControl { get; set; } = "no-cache";
}
#endif
