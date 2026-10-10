#if NET6_0_OR_GREATER
using System;

using I18Next.Net.AspNetCore.Internal;
using I18Next.Net.Backends;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.AspNetCore;

public static class EndpointRouteBuilderExtensions
{
    public const string DefaultResourcesPattern = "/locales/{lng}/{ns}.json";

    public const string DefaultMissingKeysPattern = "/locales/add/{lng}/{ns}";

    /// <summary>
    ///     Serves the namespaces of the registered translation backend as i18next JSON, so i18next in the browser can load
    ///     its translations with the <c>i18next-http-backend</c> from the application.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The language and namespace are read from the <c>lng</c> and <c>ns</c> route parameters. When the pattern
    ///         contains neither of them, they are read from the query string and several languages and namespaces separated
    ///         by <c>+</c> can be requested at once like the <c>allowMultiLoading</c> option of the <c>i18next-http-backend</c>
    ///         expects (e.g. <c>/locales/resources.json?lng=en+de&amp;ns=common+translation</c>).
    ///     </para>
    ///     <para>
    ///         Responses carry an <c>ETag</c>, so browsers revalidate cheaply. Serialized namespaces are cached until the
    ///         backend reports changed translations.
    ///     </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="configure">Configures the allowed languages and namespaces and the caching headers.</param>
    /// <returns>A builder to further configure the endpoint, e.g. with CORS or authorization.</returns>
    public static IEndpointConventionBuilder MapI18NextResources(this IEndpointRouteBuilder endpoints, string pattern = DefaultResourcesPattern,
        Action<I18NextResourcesOptions> configure = null)
    {
        if (endpoints == null)
            throw new ArgumentNullException(nameof(endpoints));
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

        var options = new I18NextResourcesOptions();
        configure?.Invoke(options);

        var routePattern = RoutePatternFactory.Parse(pattern);
        var multiLoad = routePattern.GetParameter("lng") == null && routePattern.GetParameter("ns") == null;
        var backend = endpoints.ServiceProvider.GetRequiredService<ITranslationBackend>();
        var endpoint = new ResourcesEndpoint(backend, options, multiLoad);

        return endpoints.MapMethods(pattern, [HttpMethods.Get, HttpMethods.Head], endpoint.HandleAsync)
            .WithDisplayName("I18Next resources " + pattern);
    }

    /// <summary>
    ///     Receives the missing keys which i18next in the browser reports with the <c>saveMissing</c> option of the
    ///     <c>i18next-http-backend</c> and passes them to the registered <see cref="Plugins.IMissingKeyHandler" />s.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Meant for development. Do not expose the endpoint publicly without authorization, e.g. with
    ///         <c>.RequireAuthorization()</c>, as anyone could report keys.
    ///     </para>
    ///     <para>
    ///         The endpoint expects a JSON object with the keys as property names, the values are ignored. The language and
    ///         namespace are read from the <c>lng</c> and <c>ns</c> route parameters. The sender passed to the missing key
    ///         handlers is the current <see cref="HttpContext" />.
    ///     </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="configure">Configures the allowed languages and namespaces and the request limits.</param>
    /// <returns>A builder to further configure the endpoint, e.g. with authorization.</returns>
    public static IEndpointConventionBuilder MapI18NextMissingKeys(this IEndpointRouteBuilder endpoints, string pattern = DefaultMissingKeysPattern,
        Action<I18NextMissingKeysOptions> configure = null)
    {
        if (endpoints == null)
            throw new ArgumentNullException(nameof(endpoints));
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

        var options = new I18NextMissingKeysOptions();
        configure?.Invoke(options);

        if (options.MaxRequestBodySize <= 0)
            throw new ArgumentException("The maximum request body size must be greater than zero.", nameof(configure));
        if (options.MaxKeys <= 0)
            throw new ArgumentException("The maximum number of keys must be greater than zero.", nameof(configure));

        var routePattern = RoutePatternFactory.Parse(pattern);

        if (routePattern.GetParameter("lng") == null || routePattern.GetParameter("ns") == null)
            throw new ArgumentException("The pattern must contain the {lng} and {ns} parameters.", nameof(pattern));

        var endpoint = new MissingKeysEndpoint(options);

        return endpoints.MapPost(pattern, endpoint.HandleAsync)
            .WithDisplayName("I18Next missing keys " + pattern);
    }
}
#endif
