using System;

using I18Next.Net.Extensions.Builder;
using I18Next.Net.Extensions.Configuration;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace I18Next.Net.Blazor;

public static class I18NextBuilderExtensions
{
    /// <summary>
    ///     Registers the scoped <see cref="IBlazorI18Next" /> holding the language of each user and detects the initial
    ///     language from the current culture.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <param name="configure">Configures the supported languages, the preloaded namespaces and how the language is stored.</param>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder IntegrateToBlazor(this I18NextBuilder builder, Action<I18NextBlazorOptions> configure = null)
    {
        builder.AddLanguageDetector(c => new ThreadLanguageDetector(c.GetRequiredService<IOptions<I18NextOptions>>().Value.DefaultLanguage));

        var options = builder.Services.AddOptions<I18NextBlazorOptions>();

        if (configure != null)
            options.Configure(configure);

        builder.Services.TryAddScoped<IBlazorI18Next, BlazorI18Next>();

        return builder;
    }
}
