using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace I18Next.Net.DataAnnotations;

internal sealed class ValidationAttributeMapping(Func<ValidationAttribute, string> key, Action<ValidationAttribute, IDictionary<string, object>> addArguments)
{
    private readonly Action<ValidationAttribute, IDictionary<string, object>> _addArguments = addArguments;
    private readonly Func<ValidationAttribute, string> _key = key;

    public void AddArguments(ValidationAttribute attribute, IDictionary<string, object> arguments)
    {
        _addArguments?.Invoke(attribute, arguments);
    }

    public string GetKey(ValidationAttribute attribute)
    {
        return _key(attribute);
    }
}
