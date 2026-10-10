using System;

using I18Next.Net.Extensions;
using I18Next.Net.Extensions.Builder;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;

namespace I18Next.Net.Maui;

public static class MauiAppBuilderExtensions
{
    /// <summary>
    ///     Registers I18Next with the default configuration and uses it for the <see cref="TExtension" /> markup extension.
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <returns>The MAUI app builder.</returns>
    public static MauiAppBuilder UseI18Next(this MauiAppBuilder builder)
    {
        return UseI18Next(builder, null);
    }

    /// <summary>
    ///     Registers I18Next and uses it for the <see cref="TExtension" /> markup extension.
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="i18Next">Configures backends, plugins and options.</param>
    /// <returns>The MAUI app builder.</returns>
    public static MauiAppBuilder UseI18Next(this MauiAppBuilder builder, Action<I18NextBuilder> i18Next)
    {
        builder.Services.AddI18NextLocalization(i18Next);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IMauiInitializeService, I18NextInitializer>());

        return builder;
    }

    private sealed class I18NextInitializer : IMauiInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            I18NextXaml.Instance = services.GetRequiredService<II18Next>();
        }
    }
}
