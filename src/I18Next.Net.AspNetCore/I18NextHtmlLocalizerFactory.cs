using System;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore;

public class I18NextHtmlLocalizerFactory : IHtmlLocalizerFactory
{
    private readonly IStringLocalizerFactory _factory;

    public I18NextHtmlLocalizerFactory(IStringLocalizerFactory localizerFactory)
    {
        if (localizerFactory == null)
            throw new ArgumentNullException(nameof(localizerFactory));

        _factory = localizerFactory;
    }

    public virtual IHtmlLocalizer Create(Type resourceSource)
    {
        if (resourceSource == null)
            throw new ArgumentNullException(nameof(resourceSource));

        return new I18NextHtmlLocalizer(_factory.Create(resourceSource));
    }

    public virtual IHtmlLocalizer Create(string baseName, string location)
    {
        if (baseName == null)
            throw new ArgumentNullException(nameof(baseName));
        if (location == null)
            throw new ArgumentNullException(nameof(location));

        return new I18NextHtmlLocalizer(_factory.Create(baseName, location));
    }
}