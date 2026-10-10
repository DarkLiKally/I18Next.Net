namespace I18Next.Net.AspNetCore;

/// <summary>
///     Defines how localized routes handle requests without a language prefix.
/// </summary>
public enum MissingLanguagePrefixBehavior
{
    /// <summary>
    ///     The request is passed on unchanged.
    /// </summary>
    None,

    /// <summary>
    ///     GET and HEAD requests are redirected to the localized path in the detected language. The language is taken from
    ///     the request culture if request localization ran before, from the <c>Accept-Language</c> header or else the default
    ///     language is used.
    /// </summary>
    RedirectToDetectedLanguage
}
