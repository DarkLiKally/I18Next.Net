using System;

using I18Next.Net.Extensions.Builder;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

#if NET10_0_OR_GREATER
using I18Next.Net.DataAnnotations.MinimalApis;

using Microsoft.Extensions.Validation;
#endif

namespace I18Next.Net.DataAnnotations;

public static class I18NextBuilderExtensions
{
    /// <summary>
    ///     Registers the <see cref="I18NextValidationLocalizer" /> and the <see cref="I18NextValidator" />. On .NET 10 the
    ///     validation messages of minimal APIs registered with <c>AddValidation</c> are translated too.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddDataAnnotationsLocalization(this I18NextBuilder builder)
    {
        return AddDataAnnotationsLocalization(builder, null);
    }

    /// <summary>
    ///     Registers the <see cref="I18NextValidationLocalizer" /> and the <see cref="I18NextValidator" />. On .NET 10 the
    ///     validation messages of minimal APIs registered with <c>AddValidation</c> are translated too.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <param name="configure">Configures the DataAnnotations integration.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddDataAnnotationsLocalization(this I18NextBuilder builder, Action<I18NextDataAnnotationsOptions> configure)
    {
        AddDataAnnotationsServices(builder.Services, configure);

        return builder;
    }

    internal static void AddDataAnnotationsServices(IServiceCollection services, Action<I18NextDataAnnotationsOptions> configure)
    {
        services.AddOptions<I18NextDataAnnotationsOptions>();

        if (configure != null)
            services.Configure(configure);

        services.TryAddSingleton(c =>
            new I18NextValidationLocalizer(c.GetRequiredService<II18Next>(), c.GetRequiredService<IOptions<I18NextDataAnnotationsOptions>>().Value));
        services.TryAddSingleton(c => new I18NextValidator(c.GetRequiredService<I18NextValidationLocalizer>()));

#if NET10_0_OR_GREATER
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<ValidationOptions>, I18NextValidationOptionsSetup>());
#endif
    }
}
