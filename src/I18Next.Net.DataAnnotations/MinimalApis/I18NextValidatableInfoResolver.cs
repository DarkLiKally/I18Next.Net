using System;
using System.Reflection;

using Microsoft.Extensions.Validation;

namespace I18Next.Net.DataAnnotations.MinimalApis;

internal sealed class I18NextValidatableInfoResolver(ValidationOptions options, I18NextValidationLocalizer localizer) : IValidatableInfoResolver
{
    private readonly I18NextValidationLocalizer _localizer = localizer;
    private readonly ValidationOptions _options = options;

    public bool TryGetValidatableTypeInfo(Type type, out IValidatableInfo validatableInfo)
    {
        validatableInfo = null;

        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, out IValidatableInfo validatableInfo)
    {
        foreach (var resolver in _options.Resolvers)
        {
            if (resolver == this || !resolver.TryGetValidatableParameterInfo(parameterInfo, out var parameterValidatableInfo))
                continue;

            validatableInfo = new I18NextValidatableParameterInfo(parameterValidatableInfo, parameterInfo, _localizer);

            return true;
        }

        validatableInfo = null;

        return false;
    }
}
