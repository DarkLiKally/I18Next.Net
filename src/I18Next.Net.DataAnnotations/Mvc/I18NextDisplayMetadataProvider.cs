using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace I18Next.Net.DataAnnotations.Mvc;

/// <summary>
///     Translates the display names of properties and parameters. The name of the <see cref="DisplayAttribute" />, the
///     <see cref="DisplayNameAttribute" /> or the name of the property or parameter is used as the key.
/// </summary>
public class I18NextDisplayMetadataProvider(I18NextValidationLocalizer localizer) : IDisplayMetadataProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

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
