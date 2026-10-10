using System;

using FluentValidation.Resources;

using I18Next.Net.Extensions.Builder;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace I18Next.Net.FluentValidation;

public static class I18NextBuilderExtensions
{
    /// <summary>
    ///     Registers the <see cref="I18NextLanguageManager" /> and the <see cref="I18NextDisplayNameResolver" />. They are
    ///     assigned to <c>ValidatorOptions.Global</c> when the host starts.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddFluentValidationLocalization(this I18NextBuilder builder)
    {
        return AddFluentValidationLocalization(builder, null);
    }

    /// <summary>
    ///     Registers the <see cref="I18NextLanguageManager" /> and the <see cref="I18NextDisplayNameResolver" />. They are
    ///     assigned to <c>ValidatorOptions.Global</c> when the host starts.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <param name="configure">Configures the FluentValidation integration.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddFluentValidationLocalization(this I18NextBuilder builder, Action<I18NextFluentValidationOptions> configure)
    {
        builder.Services.AddOptions<I18NextFluentValidationOptions>();

        if (configure != null)
            builder.Services.Configure(configure);

        builder.Services.TryAddSingleton(c =>
            new I18NextLanguageManager(c.GetRequiredService<II18Next>(), c.GetRequiredService<IOptions<I18NextFluentValidationOptions>>().Value));
        builder.Services.TryAddSingleton<ILanguageManager>(c => c.GetRequiredService<I18NextLanguageManager>());
        builder.Services.TryAddSingleton(c =>
            new I18NextDisplayNameResolver(c.GetRequiredService<II18Next>(), c.GetRequiredService<IOptions<I18NextFluentValidationOptions>>().Value));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, I18NextFluentValidationHostedService>());

        return builder;
    }
}
