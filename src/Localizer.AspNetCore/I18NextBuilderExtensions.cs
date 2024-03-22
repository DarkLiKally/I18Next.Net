using Localizer.Extensions.Builder;
using Localizer.Plugins;

namespace Localizer.AspNetCore;

public static class I18NextBuilderExtensions
{
    public static I18NextBuilder IntegrateToAspNetCore(this I18NextBuilder builder)
    {
        builder
            .AddLanguageDetector<ThreadLanguageDetector>()
            .AddInterpolator<HtmlInterpolator>()
            .Configure(o => o.DetectLanguageOnEachTranslation = true);

        return builder;
    }
}
