using System;
using System.Linq;

using I18Next.Net.DataAnnotations.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace I18Next.Net.DataAnnotations;

public static class MvcBuilderExtensions
{
    /// <summary>
    ///     Translates the validation messages of the DataAnnotations, the model binding messages and the display names of
    ///     ASP.NET Core MVC using I18Next. The error messages don't have to be set on the attributes.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <returns>The current MVC builder instance.</returns>
    public static IMvcBuilder AddI18NextDataAnnotationsLocalization(this IMvcBuilder builder)
    {
        return AddI18NextDataAnnotationsLocalization(builder, null);
    }

    /// <summary>
    ///     Translates the validation messages of the DataAnnotations, the model binding messages and the display names of
    ///     ASP.NET Core MVC using I18Next. The error messages don't have to be set on the attributes.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="configure">Configures the DataAnnotations integration.</param>
    /// <returns>The current MVC builder instance.</returns>
    public static IMvcBuilder AddI18NextDataAnnotationsLocalization(this IMvcBuilder builder, Action<I18NextDataAnnotationsOptions> configure)
    {
        I18NextBuilderExtensions.AddDataAnnotationsServices(builder.Services, configure);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, I18NextMvcOptionsSetup>());

        if (builder.Services.Any(s => s.ServiceType == typeof(I18NextValidationAttributeAdapterProvider)))
            return builder;

        var adapterProvider = builder.Services.LastOrDefault(IsAdapterProvider);

        builder.Services.AddSingleton(c =>
            new I18NextValidationAttributeAdapterProvider(CreateAdapterProvider(c, adapterProvider), c.GetRequiredService<I18NextValidationLocalizer>()));
        builder.Services.Replace(ServiceDescriptor.Singleton<IValidationAttributeAdapterProvider>(c =>
            c.GetRequiredService<I18NextValidationAttributeAdapterProvider>()));

        return builder;
    }

    private static IValidationAttributeAdapterProvider CreateAdapterProvider(IServiceProvider services, ServiceDescriptor descriptor)
    {
        if (descriptor?.ImplementationInstance != null)
            return (IValidationAttributeAdapterProvider)descriptor.ImplementationInstance;

        if (descriptor?.ImplementationFactory != null)
            return (IValidationAttributeAdapterProvider)descriptor.ImplementationFactory(services);

        return descriptor?.ImplementationType != null
            ? (IValidationAttributeAdapterProvider)ActivatorUtilities.CreateInstance(services, descriptor.ImplementationType)
            : new ValidationAttributeAdapterProvider();
    }

    private static bool IsAdapterProvider(ServiceDescriptor descriptor)
    {
#if NET8_0_OR_GREATER
        if (descriptor.IsKeyedService)
            return false;
#endif

        return descriptor.ServiceType == typeof(IValidationAttributeAdapterProvider);
    }
}
