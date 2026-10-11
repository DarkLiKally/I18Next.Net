using System;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Creates <see cref="I18NextHtmlLocalizer" />s which wrap the string localizers of the registered
///     <see cref="IStringLocalizerFactory" />. Registered by
///     <see cref="MvcBuilderExtensions.AddI18NextViewLocalization" />.
/// </summary>
/// <param name="localizerFactory">The factory creating the string localizers wrapped by the HTML localizers.</param>
/// <exception cref="ArgumentNullException">If <paramref name="localizerFactory" /> is <c>null</c>.</exception>
public class I18NextHtmlLocalizerFactory(IStringLocalizerFactory localizerFactory) : IHtmlLocalizerFactory
{
    private readonly IStringLocalizerFactory _factory = localizerFactory ?? throw new ArgumentNullException(nameof(localizerFactory));

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">If <paramref name="resourceSource" /> is <c>null</c>.</exception>
    public virtual IHtmlLocalizer Create(Type resourceSource)
    {
        return resourceSource == null
            ? throw new ArgumentNullException(nameof(resourceSource))
            : (IHtmlLocalizer)new I18NextHtmlLocalizer(_factory.Create(resourceSource));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    ///     If <paramref name="baseName" /> or <paramref name="location" /> is <c>null</c>.
    /// </exception>
    public virtual IHtmlLocalizer Create(string baseName, string location)
    {
        if (baseName == null)
            throw new ArgumentNullException(nameof(baseName));
        return location == null
            ? throw new ArgumentNullException(nameof(location))
            : (IHtmlLocalizer)new I18NextHtmlLocalizer(_factory.Create(baseName, location));
    }
}
