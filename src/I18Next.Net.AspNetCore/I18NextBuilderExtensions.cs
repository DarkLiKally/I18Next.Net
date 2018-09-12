using I18Next.Net.Extensions.Builder;
using I18Next.Net.Plugins;

namespace I18Next.Net.AspNetCore
{
    public static class I18NextBuilderExtensions
    {
        public static I18NextBuilder IntegrateToAspNetCore(this I18NextBuilder builder)
        {
            builder
                .UseLanguageDetector<ThreadLanguageDetector>()
                .UseInterpolator<HtmlInterpolator>()
                .Configure(i18N => i18N.DetectLanguageOnEachTranslation = true);

            return builder;
        }
    }
}
