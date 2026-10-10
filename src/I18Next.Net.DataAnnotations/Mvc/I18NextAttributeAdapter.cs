using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace I18Next.Net.DataAnnotations.Mvc;

internal sealed class I18NextAttributeAdapter(IAttributeAdapter adapter, ValidationAttribute attribute, I18NextValidationLocalizer localizer) : IAttributeAdapter
{
    private readonly IAttributeAdapter _adapter = adapter;
    private readonly ValidationAttribute _attribute = attribute;
    private readonly I18NextValidationLocalizer _localizer = localizer;

    public void AddValidation(ClientModelValidationContext context)
    {
        var existingKeys = new HashSet<string>(context.Attributes.Keys);

        _adapter.AddValidation(context);

        var originalMessage = _adapter.GetErrorMessage(context);
        var message = GetErrorMessage(context);

        if (message == originalMessage)
            return;

        foreach (var key in context.Attributes.Keys.Where(k => !existingKeys.Contains(k)).ToList())
        {
            if (context.Attributes[key] == originalMessage)
                context.Attributes[key] = message;
        }
    }

    public string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        var displayName = validationContext.ModelMetadata.GetDisplayName();
        var otherDisplayName = I18NextModelValidator.GetOtherDisplayName(validationContext, _attribute);

        return _localizer.GetErrorMessage(_attribute, displayName, otherDisplayName, null) ?? _adapter.GetErrorMessage(validationContext);
    }
}
