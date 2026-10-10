using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Determines the request culture from the language prefix of the path, e.g. <c>de</c> for <c>/de/produkte/42</c>.
///     Works before and after the localized routes middleware. Only cultures supported by the request localization are
///     used.
/// </summary>
public class PathLanguageRequestCultureProvider : RequestCultureProvider
{
    private const int MaxLanguageLength = 35;

    /// <inheritdoc />
    public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        var feature = httpContext.Features.Get<I18NextLocalizedRouteFeature>();
        var language = feature != null ? feature.Language : GetFirstSegment(httpContext.Request.Path);

        return language == null ? NullProviderCultureResult : Task.FromResult(new ProviderCultureResult(language));
    }

    private static string GetFirstSegment(PathString path)
    {
        var value = path.Value;

        if (string.IsNullOrEmpty(value) || value.Length < 2)
            return null;

        var end = value.IndexOf('/', 1);
        var length = (end < 0 ? value.Length : end) - 1;

        return length is > 0 and <= MaxLanguageLength ? value.Substring(1, length) : null;
    }
}
