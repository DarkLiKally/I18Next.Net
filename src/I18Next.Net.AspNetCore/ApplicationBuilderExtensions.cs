using System;

using I18Next.Net.AspNetCore.Internal;
using I18Next.Net.Backends;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.AspNetCore;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Serves localized routes like <c>/de/produkte/42</c> and <c>/en/products/42</c> from the canonical route
    ///     <c>/products/42</c>. The language prefix is moved to the path base, the remaining path segments are translated
    ///     back to the canonical segments and the culture of the request is set to the language of the prefix.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Add the middleware before <c>UseRouting</c>. Minimal API applications have to call <c>UseRouting</c>
    ///         explicitly after this middleware, as routing would run first otherwise.
    ///     </para>
    ///     <para>
    ///         The translated segments are loaded from the <see cref="I18NextLocalizedRoutesOptions.Namespace" /> of the
    ///         registered translation backend, keyed by the canonical segment. Segments without translation are kept.
    ///     </para>
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Configures the languages and the handling of requests without a language prefix.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseI18NextLocalizedRoutes(this IApplicationBuilder app, Action<I18NextLocalizedRoutesOptions> configure)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new I18NextLocalizedRoutesOptions();
        configure(options);

        var localizer = new I18NextRouteLocalizer(app.ApplicationServices.GetRequiredService<ITranslationBackend>(), options);

        return app.UseMiddleware<LocalizedRoutesMiddleware>(localizer, options);
    }
}
