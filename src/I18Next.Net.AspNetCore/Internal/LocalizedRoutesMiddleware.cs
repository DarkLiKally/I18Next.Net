using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace I18Next.Net.AspNetCore.Internal;

internal sealed class LocalizedRoutesMiddleware
{
    private readonly Dictionary<string, CultureInfo> _cultures = new(StringComparer.OrdinalIgnoreCase);
    private readonly PathString[] _excludedPaths;
    private readonly I18NextRouteLocalizer _localizer;
    private readonly MissingLanguagePrefixBehavior _missingLanguagePrefix;
    private readonly RequestDelegate _next;

    public LocalizedRoutesMiddleware(RequestDelegate next, I18NextRouteLocalizer localizer, I18NextLocalizedRoutesOptions options)
    {
        _next = next;
        _localizer = localizer;
        _missingLanguagePrefix = options.MissingLanguagePrefix;
        _excludedPaths = options.ExcludedPaths?.ToArray() ?? [];

        foreach (var language in localizer.Languages)
            _cultures[language] = new CultureInfo(language);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _localizer.LoadAsync().ConfigureAwait(false);

        var request = context.Request;
        var pathBase = request.PathBase;

        if (!_localizer.TryGetLanguagePrefix(request.Path, out var language, out var matched, out var remaining))
        {
            context.Features.Set(new I18NextLocalizedRouteFeature(_localizer, null, pathBase));

            if (ShouldRedirect(request))
            {
                var location = await _localizer.LocalizePathAsync(request.Path.ToUriComponent(), DetectLanguage(context)).ConfigureAwait(false);

                context.Response.Redirect(pathBase.ToUriComponent() + location + request.QueryString.ToUriComponent());

                return;
            }

            await _next(context).ConfigureAwait(false);

            return;
        }

        var path = request.Path;
        var culture = _cultures[language];

        context.Features.Set(new I18NextLocalizedRouteFeature(_localizer, language, pathBase));
        context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture(culture), null));
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            request.PathBase = pathBase.Add(matched);
            request.Path = await _localizer.GetCanonicalPathAsync(remaining, language).ConfigureAwait(false);

            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            request.PathBase = pathBase;
            request.Path = path;
        }
    }

    private bool ShouldRedirect(HttpRequest request)
    {
        if (_missingLanguagePrefix != MissingLanguagePrefixBehavior.RedirectToDetectedLanguage)
            return false;

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            return false;

        foreach (var excludedPath in _excludedPaths)
        {
            if (request.Path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private string DetectLanguage(HttpContext context)
    {
        var cultureFeature = context.Features.Get<IRequestCultureFeature>();
        var language = cultureFeature?.Provider != null ? _localizer.MatchLanguage(cultureFeature.RequestCulture.UICulture.Name) : null;

        if (language != null)
            return language;

        var acceptLanguages = context.Request.GetTypedHeaders().AcceptLanguage;

        if (acceptLanguages != null)
        {
            foreach (var acceptLanguage in acceptLanguages.Where(l => l.Quality is null or > 0).OrderByDescending(l => l.Quality ?? 1))
            {
                language = _localizer.MatchLanguage(acceptLanguage.Value.Value);

                if (language != null)
                    return language;
            }
        }

        return _localizer.DefaultLanguage;
    }
}
