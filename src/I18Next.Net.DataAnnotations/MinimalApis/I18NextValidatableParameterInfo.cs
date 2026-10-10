using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Validation;

namespace I18Next.Net.DataAnnotations.MinimalApis;

internal sealed class I18NextValidatableParameterInfo(IValidatableInfo validatableInfo, ParameterInfo parameterInfo, I18NextValidationLocalizer localizer)
    : IValidatableInfo
{
    private static readonly ConcurrentDictionary<(Type Type, string Name), ValidatableMember> Members = new();
    private static readonly ConcurrentDictionary<Type, ValidationAttribute[]> TypeAttributes = new();

    private readonly I18NextValidationLocalizer _localizer = localizer;
    private readonly Lazy<ValidationAttribute[]> _parameterAttributes = new(() => parameterInfo.GetCustomAttributes<ValidationAttribute>(true).ToArray());
    private readonly IValidatableInfo _validatableInfo = validatableInfo;

    public async Task ValidateAsync(object value, ValidateContext context, CancellationToken cancellationToken)
    {
        Action<ValidationErrorContext> translateErrors = error => TranslateErrors(context, error, value);

        context.OnValidationError += translateErrors;

        try
        {
            await _validatableInfo.ValidateAsync(value, context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            context.OnValidationError -= translateErrors;
        }
    }

    private static ValidatableMember CreateMember((Type Type, string Name) key)
    {
        var property = key.Type.GetProperties().FirstOrDefault(p => p.Name == key.Name && p.GetIndexParameters().Length == 0);
        var parameter = key.Type.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .FirstOrDefault(p => string.Equals(p.Name, key.Name, StringComparison.OrdinalIgnoreCase));

        var attributes = new List<ValidationAttribute>();
        var displayAttribute = property?.GetCustomAttribute<DisplayAttribute>() ?? parameter?.GetCustomAttribute<DisplayAttribute>();

        if (property != null)
            attributes.AddRange(property.GetCustomAttributes<ValidationAttribute>(true));

        if (parameter != null)
            attributes.AddRange(parameter.GetCustomAttributes<ValidationAttribute>(true));

        return new ValidatableMember(property, [.. attributes], displayAttribute?.ResourceType == null ? displayAttribute?.Name : null);
    }

    private static IEnumerable<(ValidationAttribute Attribute, object Value)> GetCandidates(ValidationErrorContext error, ValidatableMember member,
        ValidationAttribute[] parameterAttributes, object value)
    {
        if (error.Container == null)
            return parameterAttributes.Select(a => (a, value));

        var typeAttributes = TypeAttributes.GetOrAdd(error.Container.GetType(), t => t.GetCustomAttributes<ValidationAttribute>(true).ToArray());
        var propertyValue = member.Property?.GetValue(error.Container);

        return member.Attributes.Select(a => (a, propertyValue)).Concat(typeAttributes.Select(a => (a, error.Container)));
    }

    private static bool ProducesMessage(ValidationAttribute attribute, object value, ValidationContext validationContext, string message)
    {
        try
        {
            return attribute.GetValidationResult(value, validationContext)?.ErrorMessage == message;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void TranslateErrors(ValidateContext context, ValidationErrorContext error, object value)
    {
        if (context.ValidationErrors == null || !context.ValidationErrors.TryGetValue(error.Path, out var messages))
            return;

        var member = error.Container == null ? null : Members.GetOrAdd((error.Container.GetType(), error.Name), CreateMember);
        var candidates = GetCandidates(error, member, _parameterAttributes.Value, value).ToList();
        var displayName = context.ValidationContext.DisplayName;
        string translatedDisplayName = null;

        foreach (var message in error.Errors.ToArray())
        {
            var attribute = candidates.FirstOrDefault(c => ProducesMessage(c.Attribute, c.Value, context.ValidationContext, message)).Attribute;
            var index = attribute == null ? -1 : Array.LastIndexOf(messages, message);

            if (index < 0)
                continue;

            translatedDisplayName ??= _localizer.GetDisplayName(member?.DisplayName ?? displayName);
            messages[index] = _localizer.GetErrorMessage(attribute, translatedDisplayName) ?? attribute.FormatErrorMessage(translatedDisplayName);
        }
    }

    private sealed class ValidatableMember(PropertyInfo property, ValidationAttribute[] attributes, string displayName)
    {
        public ValidationAttribute[] Attributes { get; } = attributes;

        public string DisplayName { get; } = displayName;

        public PropertyInfo Property { get; } = property;
    }
}
