using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace I18Next.Net.DataAnnotations.Mvc;

/// <summary>
///     Creates the server side validators of validation attributes whose error messages are translated. It has to run
///     before the DataAnnotations validator provider of ASP.NET Core MVC.
/// </summary>
public class I18NextModelValidatorProvider(I18NextValidationLocalizer localizer) : IMetadataBasedModelValidatorProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    public void CreateValidators(ModelValidatorProviderContext context)
    {
        var results = context.Results;

        for (var i = 0; i < results.Count; i++)
        {
            var validatorItem = results[i];

            if (validatorItem.Validator != null || validatorItem.ValidatorMetadata is not ValidationAttribute attribute || !_localizer.CanTranslate(attribute))
                continue;

            validatorItem.Validator = new I18NextModelValidator(_localizer, attribute);
            validatorItem.IsReusable = true;

            if (attribute is not RequiredAttribute || i == 0)
                continue;

            results.RemoveAt(i);
            results.Insert(0, validatorItem);
        }
    }

    public bool HasValidators(Type modelType, IList<object> validatorMetadata)
    {
        return validatorMetadata.OfType<ValidationAttribute>().Any();
    }
}
