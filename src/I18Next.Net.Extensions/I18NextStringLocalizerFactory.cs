using System;

using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions;

public class I18NextStringLocalizerFactory(II18Next i18NextNet) : IStringLocalizerFactory
{
    private readonly II18Next _i18NextNet = i18NextNet;

    public IStringLocalizer Create(Type resourceSource)
    {
        return new I18NextStringLocalizer(_i18NextNet);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new I18NextStringLocalizer(_i18NextNet);
    }
}
