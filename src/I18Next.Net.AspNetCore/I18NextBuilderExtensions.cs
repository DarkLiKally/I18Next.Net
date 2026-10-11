using I18Next.Net.Extensions.Builder;
using I18Next.Net.Plugins;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Extension methods for <see cref="I18NextBuilder" /> to integrate I18Next into ASP.NET Core.
/// </summary>
public static class I18NextBuilderExtensions
{
    /// <summary>
    ///     Configures I18Next for ASP.NET Core: registers a <see cref="ThreadLanguageDetector" />, which detects the
    ///     language from the current culture of the thread (set per request e.g. by the request localization
    ///     middleware), the <see cref="HtmlInterpolator" />, which HTML escapes the interpolated values, and enables
    ///     <see cref="Extensions.Configuration.I18NextOptions.DetectLanguageOnEachTranslation" />, so every translation
    ///     uses the language of the current request.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <returns>The I18Next builder.</returns>
    public static I18NextBuilder IntegrateToAspNetCore(this I18NextBuilder builder)
    {
        builder
            .AddLanguageDetector<ThreadLanguageDetector>()
            .AddInterpolator<HtmlInterpolator>()
            .Configure(o => o.DetectLanguageOnEachTranslation = true);

        return builder;
    }
}
