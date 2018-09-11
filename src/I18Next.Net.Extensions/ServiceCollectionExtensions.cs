using System;
using I18Next.Net.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddI18Next(this IServiceCollection services, Action<I18NextBuilder> i18Next)
        {
            var builder = new I18NextBuilder();

            i18Next?.Invoke(builder);

            var instance = builder.Build();
            var localizer = new I18NextStringLocalizer(instance);

            services.AddSingleton(instance);
            services.AddSingleton(builder.Backend);
            services.AddSingleton(builder.Interpolator);
            services.AddSingleton(builder.Translator);
            services.AddSingleton(builder.PluralResolver);
            services.AddSingleton(builder.LanguageDetector);
            services.AddSingleton<IStringLocalizer>(localizer);

            return services;
        }
    }
}
