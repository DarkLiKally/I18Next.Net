using System;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore;

public class I18NextHtmlLocalizerFactory(IStringLocalizerFactory localizerFactory) : IHtmlLocalizerFactory
{
    private readonly IStringLocalizerFactory _factory = localizerFactory ?? throw new ArgumentNullException(nameof(localizerFactory));

    public virtual IHtmlLocalizer Create(Type resourceSource)
    {
        return resourceSource == null
            ? throw new ArgumentNullException(nameof(resourceSource))
            : (IHtmlLocalizer)new I18NextHtmlLocalizer(_factory.Create(resourceSource));
    }

    public virtual IHtmlLocalizer Create(string baseName, string location)
    {
        if (baseName == null)
            throw new ArgumentNullException(nameof(baseName));
        return location == null
            ? throw new ArgumentNullException(nameof(location))
            : (IHtmlLocalizer)new I18NextHtmlLocalizer(_factory.Create(baseName, location));
    }
}
