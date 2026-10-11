using System;

using I18Next.Net.Extensions.Builder;

using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.Extensions;

/// <summary>
///     Extension methods registering I18Next.Net in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers I18Next.Net with the default plugins: a <see cref="I18Next.Net.Backends.JsonFileBackend" />
    ///     reading the <c>locales/{lng}/{ns}.json</c> files, the default translator, interpolator, plural resolver and
    ///     language detector, and a logger writing to the registered <c>Microsoft.Extensions.Logging.ILogger</c>.
    ///     Registers <see cref="II18Next" />, <see cref="II18NextFactory" />, <c>IStringLocalizerFactory</c>,
    ///     <c>IStringLocalizer</c> and <c>IStringLocalizer&lt;T&gt;</c>.
    /// </summary>
    /// <param name="services">The service collection to register the services in.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddI18NextLocalization(this IServiceCollection services)
    {
        return AddI18NextLocalization(services, null);
    }

    /// <summary>
    ///     Registers I18Next.Net with the plugins and options configured with an <see cref="I18NextBuilder" />. Plugins
    ///     which are not configured are registered with the defaults of
    ///     <see cref="AddI18NextLocalization(IServiceCollection)" />.
    /// </summary>
    /// <param name="services">The service collection to register the services in.</param>
    /// <param name="i18Next">Configures the plugins and options, may be <c>null</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddI18NextLocalization(this IServiceCollection services, Action<I18NextBuilder> i18Next)
    {
        var builder = new I18NextBuilder(services);

        i18Next?.Invoke(builder);

        builder.Build();

        return services;
    }
}
