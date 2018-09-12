using System;
using I18Next.Net.Extensions.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddI18NextLocalization(this IServiceCollection services)
        {
            return AddI18NextLocalization(services, null);
        }
        
        public static IServiceCollection AddI18NextLocalization(this IServiceCollection services, Action<I18NextBuilder> i18Next)
        {
            var builder = new I18NextBuilder(services);

            i18Next?.Invoke(builder);

            var instance = builder.Build();

            services.AddSingleton(instance);

            builder.ConfigureOptions(options =>
            {
                services.AddSingleton(options.Backend);
                services.AddSingleton(options.Interpolator);
                services.AddSingleton(options.Translator);
                services.AddSingleton(options.PluralResolver);
                services.AddSingleton(options.LanguageDetector);
            });

            services.AddSingleton<IStringLocalizerFactory, I18NextStringLocalizerFactory>();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

            return services;
        }
    }
}
