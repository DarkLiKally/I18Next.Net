using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.DataAnnotations.Mvc;

/// <summary>
///     Decorates the adapters of another provider to translate the client side validation messages.
/// </summary>
public class I18NextValidationAttributeAdapterProvider(IValidationAttributeAdapterProvider provider, I18NextValidationLocalizer localizer)
    : IValidationAttributeAdapterProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    private readonly IValidationAttributeAdapterProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
    {
        var adapter = _provider.GetAttributeAdapter(attribute, stringLocalizer);

        return adapter != null && _localizer.CanTranslate(attribute) ? new I18NextAttributeAdapter(adapter, attribute, _localizer) : adapter;
    }
}
