using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace I18Next.Net.DataAnnotations.Mvc;

/// <summary>
///     Translates the display names of properties and parameters. The name of the <see cref="DisplayAttribute" />, the
///     <see cref="DisplayNameAttribute" /> or the name of the property or parameter is used as the key. Display attributes
///     with a <see cref="DisplayAttribute.ResourceType" /> are left to the resource based localization.
/// </summary>
/// <param name="localizer">The localizer translating the display names.</param>
/// <exception cref="ArgumentNullException">If <paramref name="localizer" /> is <c>null</c>.</exception>
public class I18NextDisplayMetadataProvider(I18NextValidationLocalizer localizer) : IDisplayMetadataProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    /// <summary>
    ///     Sets the display name of a property or parameter to a translation which is resolved each time the display name
    ///     is read, so it follows the current language. Does nothing if
    ///     <see cref="I18NextDataAnnotationsOptions.TranslateDisplayNames" /> is disabled.
    /// </summary>
    /// <param name="context">The context of the display metadata.</param>
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (!_localizer.Options.TranslateDisplayNames || context.Key.MetadataKind == ModelMetadataKind.Type || string.IsNullOrEmpty(context.Key.Name))
            return;

        var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();

        if (displayAttribute?.ResourceType != null)
            return;

        var key = displayAttribute?.Name;

        if (string.IsNullOrEmpty(key))
            key = context.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName;

        if (string.IsNullOrEmpty(key))
            key = context.Key.Name;

        context.DisplayMetadata.DisplayName = () => _localizer.GetDisplayName(key);
    }
}
