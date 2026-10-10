using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace I18Next.Net.DataAnnotations.Mvc;

internal sealed class I18NextModelValidator(I18NextValidationLocalizer localizer, ValidationAttribute attribute) : IModelValidator
{
    private static readonly object EmptyValidationContextInstance = new();

    private readonly ValidationAttribute _attribute = attribute;
    private readonly I18NextValidationLocalizer _localizer = localizer;

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext)
    {
        var metadata = validationContext.ModelMetadata;
        var memberName = metadata.Name;
        var displayName = metadata.GetDisplayName();

        var context = new ValidationContext(validationContext.Container ?? validationContext.Model ?? EmptyValidationContextInstance,
            validationContext.ActionContext?.HttpContext?.RequestServices, null)
        {
            DisplayName = displayName,
            MemberName = memberName
        };

        var result = _attribute.GetValidationResult(validationContext.Model, context);

        if (result == ValidationResult.Success)
            return [];

        var errorMessage = _localizer.GetErrorMessage(_attribute, displayName, GetOtherDisplayName(validationContext, _attribute), null) ?? result.ErrorMessage;
        var results = new List<ModelValidationResult>();

        if (result.MemberNames != null)
            foreach (var resultMemberName in result.MemberNames)
                results.Add(new ModelValidationResult(string.Equals(resultMemberName, memberName, StringComparison.Ordinal) ? null : resultMemberName,
                    errorMessage));

        if (results.Count == 0)
            results.Add(new ModelValidationResult(null, errorMessage));

        return results;
    }

    internal static string GetOtherDisplayName(ModelValidationContextBase context, ValidationAttribute attribute)
    {
        if (attribute is not CompareAttribute compareAttribute || context.ModelMetadata.ContainerType == null)
            return null;

        return context.MetadataProvider.GetMetadataForType(context.ModelMetadata.ContainerType).Properties[compareAttribute.OtherProperty]?.GetDisplayName();
    }
}
