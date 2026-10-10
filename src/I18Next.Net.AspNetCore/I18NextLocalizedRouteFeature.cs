using Microsoft.AspNetCore.Http;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Describes the localized route of the current request. Set by the localized routes middleware.
/// </summary>
public class I18NextLocalizedRouteFeature(I18NextRouteLocalizer localizer, string language, PathString pathBase)
{
    /// <summary>
    ///     The localizer used to translate the paths.
    /// </summary>
    public I18NextRouteLocalizer Localizer { get; } = localizer;

    /// <summary>
    ///     The language of the path prefix or <c>null</c> if the request has no language prefix.
    /// </summary>
    public string Language { get; } = language;

    /// <summary>
    ///     The path base of the request without the language prefix.
    /// </summary>
    public PathString PathBase { get; } = pathBase;
}
