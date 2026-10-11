using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.DataAnnotations.Mvc;

/// <summary>
///     Decorates the adapters of another provider to translate the client side validation messages.
/// </summary>
/// <param name="provider">
///     The provider whose adapters are decorated, usually the default provider of ASP.NET Core MVC.
/// </param>
/// <param name="localizer">The localizer translating the error messages.</param>
/// <exception cref="ArgumentNullException">
///     If <paramref name="provider" /> or <paramref name="localizer" /> is <c>null</c>.
/// </exception>
public class I18NextValidationAttributeAdapterProvider(IValidationAttributeAdapterProvider provider, I18NextValidationLocalizer localizer)
    : IValidationAttributeAdapterProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    private readonly IValidationAttributeAdapterProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    ///     Returns the adapter of the decorated provider, wrapped to translate its error message if the error message of the
    ///     attribute is translated.
    /// </summary>
    /// <param name="attribute">The validation attribute.</param>
    /// <param name="stringLocalizer">The string localizer passed to the decorated provider.</param>
    /// <returns>The adapter or <c>null</c> if the decorated provider has no adapter for the attribute.</returns>
    public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
    {
        var adapter = _provider.GetAttributeAdapter(attribute, stringLocalizer);

        return adapter != null && _localizer.CanTranslate(attribute) ? new I18NextAttributeAdapter(adapter, attribute, _localizer) : adapter;
    }
}
