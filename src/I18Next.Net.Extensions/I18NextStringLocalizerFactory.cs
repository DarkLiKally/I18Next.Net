using System;

using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions;

/// <summary>
///     Creates <see cref="I18NextStringLocalizer" /> instances translating with the registered <see cref="II18Next" />
///     instance. All localizers use its default namespace, independent of the requested resource.
/// </summary>
/// <param name="i18NextNet">The instance the localizers translate with.</param>
public class I18NextStringLocalizerFactory(II18Next i18NextNet) : IStringLocalizerFactory
{
    private readonly II18Next _i18NextNet = i18NextNet;

    /// <summary>
    ///     Creates a localizer translating in the default namespace of the instance.
    /// </summary>
    /// <param name="resourceSource">The type requesting the localizer. Ignored.</param>
    /// <returns>A new localizer.</returns>
    public IStringLocalizer Create(Type resourceSource)
    {
        return new I18NextStringLocalizer(_i18NextNet);
    }

    /// <summary>
    ///     Creates a localizer translating in the default namespace of the instance.
    /// </summary>
    /// <param name="baseName">The base name of the resource. Ignored.</param>
    /// <param name="location">The location of the resource. Ignored.</param>
    /// <returns>A new localizer.</returns>
    public IStringLocalizer Create(string baseName, string location)
    {
        return new I18NextStringLocalizer(_i18NextNet);
    }
}
