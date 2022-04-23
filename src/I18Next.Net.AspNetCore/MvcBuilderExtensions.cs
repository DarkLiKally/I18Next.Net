using System.Linq;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace I18Next.Net.AspNetCore;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddI18NextViewLocalization(this IMvcBuilder builder)
    {
        var viewLocalizerType = typeof(IViewLocalizer);

        if (builder.Services.All(x => x.ServiceType != viewLocalizerType))
            builder.AddViewLocalization();

        var factoryType = typeof(IHtmlLocalizerFactory);

        var factory = builder.Services.FirstOrDefault(x => x.ServiceType == factoryType);
        if (factory != null)
            builder.Services.Remove(factory);

        builder.Services.TryAdd(ServiceDescriptor.Singleton<IHtmlLocalizerFactory, I18NextHtmlLocalizerFactory>());

        return builder;
    }
}