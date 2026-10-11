using System.Linq;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Extension methods for <see cref="IMvcBuilder" /> to use I18Next in MVC views.
/// </summary>
public static class MvcBuilderExtensions
{
    /// <summary>
    ///     Adds the MVC view localization if it is not registered yet and replaces the registered
    ///     <see cref="IHtmlLocalizerFactory" /> with the <see cref="I18NextHtmlLocalizerFactory" />, so the
    ///     <see cref="IViewLocalizer" /> and <see cref="IHtmlLocalizer{TResource}" /> translate with I18Next and pass the
    ///     arguments to the I18Next interpolation.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <returns>The MVC builder.</returns>
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
