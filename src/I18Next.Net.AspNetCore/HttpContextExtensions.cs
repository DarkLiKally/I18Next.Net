using System;
using System.Globalization;

using Microsoft.AspNetCore.Http;

namespace I18Next.Net.AspNetCore;

public static class HttpContextExtensions
{
    /// <summary>
    ///     Localizes a canonical path of the application, e.g. <c>/products/42</c> becomes <c>/de/produkte/42</c>. The path
    ///     base of the request is prepended.
    /// </summary>
    /// <param name="context">The current http context.</param>
    /// <param name="path">The canonical, URL encoded path starting with a slash. A query string and fragment are kept.</param>
    /// <param name="language">
    ///     The language of the localized path. Defaults to the language of the current request, the current UI culture or
    ///     the default language.
    /// </param>
    /// <returns>The URL encoded localized path.</returns>
    /// <exception cref="InvalidOperationException">If the localized routes middleware did not run for the request.</exception>
    public static string GetLocalizedPath(this HttpContext context, string path, string language = null)
    {
        var feature = GetFeature(context);

        language ??= feature.Language ?? feature.Localizer.MatchLanguage(CultureInfo.CurrentUICulture.Name) ?? feature.Localizer.DefaultLanguage;

        return feature.PathBase.ToUriComponent() + feature.Localizer.LocalizePath(path, language);
    }

    /// <summary>
    ///     Localizes the path and query string of the current request for another language, e.g. for a language switcher.
    /// </summary>
    /// <param name="context">The current http context.</param>
    /// <param name="language">The language of the localized path.</param>
    /// <returns>The URL encoded localized path.</returns>
    /// <exception cref="InvalidOperationException">If the localized routes middleware did not run for the request.</exception>
    public static string GetLocalizedRequestPath(this HttpContext context, string language)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return context.GetLocalizedPath(context.Request.Path.ToUriComponent() + context.Request.QueryString.ToUriComponent(), language);
    }

    private static I18NextLocalizedRouteFeature GetFeature(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return context.Features.Get<I18NextLocalizedRouteFeature>()
               ?? throw new InvalidOperationException("Localized routes are not available. Please add UseI18NextLocalizedRoutes to the request pipeline.");
    }
}
