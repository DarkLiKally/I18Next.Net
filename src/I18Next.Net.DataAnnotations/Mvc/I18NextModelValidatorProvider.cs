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
/// <param name="localizer">The localizer translating the error messages.</param>
/// <exception cref="ArgumentNullException">If <paramref name="localizer" /> is <c>null</c>.</exception>
public class I18NextModelValidatorProvider(I18NextValidationLocalizer localizer) : IMetadataBasedModelValidatorProvider
{
    private readonly I18NextValidationLocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    /// <summary>
    ///     Creates the validators of the validation attributes which have no validator yet and whose error messages are
    ///     translated. The validator of a <see cref="RequiredAttribute" /> is moved to the front, like ASP.NET Core MVC does.
    /// </summary>
    /// <param name="context">The context containing the validator items to fill.</param>
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

    /// <summary>
    ///     Checks whether the metadata contains a validation attribute.
    /// </summary>
    /// <param name="modelType">The type of the model.</param>
    /// <param name="validatorMetadata">The metadata of the model.</param>
    /// <returns>Whether the metadata contains a <see cref="ValidationAttribute" />.</returns>
    public bool HasValidators(Type modelType, IList<object> validatorMetadata)
    {
        return validatorMetadata.OfType<ValidationAttribute>().Any();
    }
}
