using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace I18Next.Net.DataAnnotations;

/// <summary>
///     Validates objects, properties and values like the <see cref="Validator" /> and translates the error messages and
///     display names using I18Next.
/// </summary>
public class I18NextValidator
{
    private readonly I18NextValidationLocalizer _localizer;

    /// <summary>
    ///     Constructor using the default options.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the messages.</param>
    public I18NextValidator(II18Next i18Next)
        : this(new I18NextValidationLocalizer(i18Next))
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="localizer">The localizer used to translate the messages.</param>
    public I18NextValidator(I18NextValidationLocalizer localizer)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    ///     Determines whether the object is valid by checking the required attributes of its properties, its validation
    ///     attributes and <see cref="IValidatableObject.Validate" />.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="validationContext">The context describing the object to validate.</param>
    /// <param name="validationResults">Collection receiving the translated results of the failed validations.</param>
    /// <returns>Whether the object is valid.</returns>
    public bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults)
    {
        return TryValidateObject(instance, validationContext, validationResults, false);
    }

    /// <summary>
    ///     Determines whether the object is valid by checking the validation attributes of its properties, its validation
    ///     attributes and <see cref="IValidatableObject.Validate" />.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="validationContext">The context describing the object to validate.</param>
    /// <param name="validationResults">Collection receiving the translated results of the failed validations.</param>
    /// <param name="validateAllProperties">Validates all attributes of the properties instead of only the required ones.</param>
    /// <returns>Whether the object is valid.</returns>
    public bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults,
        bool validateAllProperties)
    {
        EnsureInstanceMatchesContext(instance, validationContext);

        return AddResults(GetObjectValidationErrors(instance, validationContext, validateAllProperties, validationResults == null), validationResults);
    }

    /// <summary>
    ///     Determines whether the value is valid for the property described by <see cref="ValidationContext.MemberName" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context describing the property to validate.</param>
    /// <param name="validationResults">Collection receiving the translated results of the failed validations.</param>
    /// <returns>Whether the value is valid.</returns>
    public bool TryValidateProperty(object value, ValidationContext validationContext, ICollection<ValidationResult> validationResults)
    {
        var attributes = GetPropertyValidationAttributes(value, validationContext);
        var context = CreateContext(validationContext, validationContext.MemberName, validationContext.DisplayName);

        return AddResults(GetValidationErrors(value, context, attributes, validationResults == null), validationResults);
    }

    /// <summary>
    ///     Determines whether the value is valid for the given validation attributes.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context describing the value to validate.</param>
    /// <param name="validationResults">Collection receiving the translated results of the failed validations.</param>
    /// <param name="validationAttributes">The validation attributes to check.</param>
    /// <returns>Whether the value is valid.</returns>
    public bool TryValidateValue(object value, ValidationContext validationContext, ICollection<ValidationResult> validationResults,
        IEnumerable<ValidationAttribute> validationAttributes)
    {
        if (validationContext == null)
            throw new ArgumentNullException(nameof(validationContext));
        if (validationAttributes == null)
            throw new ArgumentNullException(nameof(validationAttributes));

        var context = CreateContext(validationContext, validationContext.MemberName, validationContext.DisplayName);

        return AddResults(GetValidationErrors(value, context, validationAttributes, validationResults == null), validationResults);
    }

    /// <summary>
    ///     Validates the required attributes of the properties, the validation attributes and
    ///     <see cref="IValidatableObject.Validate" /> of the object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="validationContext">The context describing the object to validate.</param>
    /// <exception cref="ValidationException">With the translated message of the first failed validation.</exception>
    public void ValidateObject(object instance, ValidationContext validationContext)
    {
        ValidateObject(instance, validationContext, false);
    }

    /// <summary>
    ///     Validates the validation attributes of the properties, the validation attributes and
    ///     <see cref="IValidatableObject.Validate" /> of the object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="validationContext">The context describing the object to validate.</param>
    /// <param name="validateAllProperties">Validates all attributes of the properties instead of only the required ones.</param>
    /// <exception cref="ValidationException">With the translated message of the first failed validation.</exception>
    public void ValidateObject(object instance, ValidationContext validationContext, bool validateAllProperties)
    {
        EnsureInstanceMatchesContext(instance, validationContext);

        ThrowFirstError(GetObjectValidationErrors(instance, validationContext, validateAllProperties, false));
    }

    /// <summary>
    ///     Validates the value for the property described by <see cref="ValidationContext.MemberName" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context describing the property to validate.</param>
    /// <exception cref="ValidationException">With the translated message of the first failed validation.</exception>
    public void ValidateProperty(object value, ValidationContext validationContext)
    {
        var attributes = GetPropertyValidationAttributes(value, validationContext);
        var context = CreateContext(validationContext, validationContext.MemberName, validationContext.DisplayName);

        ThrowFirstError(GetValidationErrors(value, context, attributes, false));
    }

    /// <summary>
    ///     Validates the value for the given validation attributes.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context describing the value to validate.</param>
    /// <param name="validationAttributes">The validation attributes to check.</param>
    /// <exception cref="ValidationException">With the translated message of the first failed validation.</exception>
    public void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes)
    {
        if (validationContext == null)
            throw new ArgumentNullException(nameof(validationContext));
        if (validationAttributes == null)
            throw new ArgumentNullException(nameof(validationAttributes));

        var context = CreateContext(validationContext, validationContext.MemberName, validationContext.DisplayName);

        ThrowFirstError(GetValidationErrors(value, context, validationAttributes, false));
    }

    private static bool AddResults(List<ValidationError> errors, ICollection<ValidationResult> validationResults)
    {
        if (validationResults != null)
            foreach (var error in errors)
                validationResults.Add(error.Result);

        return errors.Count == 0;
    }

    private static void EnsureInstanceMatchesContext(object instance, ValidationContext validationContext)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (validationContext == null)
            throw new ArgumentNullException(nameof(validationContext));
        if (instance != validationContext.ObjectInstance)
            throw new ArgumentException("The instance provided must match the ObjectInstance on the ValidationContext supplied.", nameof(instance));
    }

    private static IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(object value, ValidationContext validationContext)
    {
        if (validationContext == null)
            throw new ArgumentNullException(nameof(validationContext));

        var property = string.IsNullOrEmpty(validationContext.MemberName)
            ? null
            : TypeDescriptor.GetProperties(validationContext.ObjectType).Find(validationContext.MemberName, false);

        if (property == null)
            throw new ArgumentException(
                $"The type '{validationContext.ObjectType.Name}' does not contain a public property named '{validationContext.MemberName}'.",
                nameof(validationContext));

        var propertyType = property.PropertyType;
        var isValidValue = value == null
            ? !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null
            : propertyType.IsInstanceOfType(value);

        if (!isValidValue)
            throw new ArgumentException($"The value for property '{property.Name}' must be of type '{propertyType}'.", nameof(value));

        return property.Attributes.OfType<ValidationAttribute>();
    }

    private static void ThrowFirstError(List<ValidationError> errors)
    {
        if (errors.Count > 0)
            throw new ValidationException(errors[0].Result, errors[0].Attribute, errors[0].Value);
    }

    private ValidationContext CreateContext(ValidationContext validationContext, string memberName, string displayName = null)
    {
        var context = new ValidationContext(validationContext.ObjectInstance, validationContext, validationContext.Items) { MemberName = memberName };
        context.DisplayName = _localizer.GetDisplayName(displayName ?? context.DisplayName);

        return context;
    }

    private List<ValidationError> GetObjectValidationErrors(object instance, ValidationContext validationContext, bool validateAllProperties,
        bool breakOnFirstError)
    {
        var errors = GetObjectPropertyValidationErrors(instance, validationContext, validateAllProperties, breakOnFirstError);

        if (errors.Count > 0)
            return errors;

        var typeAttributes = TypeDescriptor.GetAttributes(validationContext.ObjectType).OfType<ValidationAttribute>().ToList();

        if (typeAttributes.Count > 0)
            errors.AddRange(GetValidationErrors(instance, CreateContext(validationContext, null, validationContext.DisplayName), typeAttributes,
                breakOnFirstError));

        if (errors.Count > 0 || instance is not IValidatableObject validatable)
            return errors;

        var results = validatable.Validate(validationContext);

        if (results != null)
            foreach (var result in results)
            {
                if (result != ValidationResult.Success)
                    errors.Add(new ValidationError(null, instance, result));
            }

        return errors;
    }

    private List<ValidationError> GetObjectPropertyValidationErrors(object instance, ValidationContext validationContext, bool validateAllProperties,
        bool breakOnFirstError)
    {
        var errors = new List<ValidationError>();

        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(instance))
        {
            var attributes = property.Attributes.OfType<ValidationAttribute>().ToList();

            if (attributes.Count == 0)
                continue;

            var context = CreateContext(validationContext, property.Name);
            var value = property.GetValue(instance);

            if (validateAllProperties)
            {
                errors.AddRange(GetValidationErrors(value, context, attributes, breakOnFirstError));
            }
            else
            {
                var requiredAttribute = attributes.OfType<RequiredAttribute>().FirstOrDefault();
                var error = requiredAttribute == null ? null : Validate(value, context, requiredAttribute);

                if (error != null)
                    errors.Add(error);
            }

            if (breakOnFirstError && errors.Count > 0)
                break;
        }

        return errors;
    }

    private List<ValidationError> GetValidationErrors(object value, ValidationContext context, IEnumerable<ValidationAttribute> attributes,
        bool breakOnFirstError)
    {
        var errors = new List<ValidationError>();
        var attributeList = attributes as IList<ValidationAttribute> ?? attributes.ToList();
        var requiredAttribute = attributeList.OfType<RequiredAttribute>().FirstOrDefault();

        if (requiredAttribute != null)
        {
            var requiredError = Validate(value, context, requiredAttribute);

            if (requiredError != null)
            {
                errors.Add(requiredError);

                return errors;
            }
        }

        foreach (var attribute in attributeList)
        {
            if (attribute == requiredAttribute)
                continue;

            var error = Validate(value, context, attribute);

            if (error == null)
                continue;

            errors.Add(error);

            if (breakOnFirstError)
                break;
        }

        return errors;
    }

    private ValidationError Validate(object value, ValidationContext context, ValidationAttribute attribute)
    {
        var result = attribute.GetValidationResult(value, context);

        if (result == ValidationResult.Success)
            return null;

        var message = _localizer.GetErrorMessage(attribute, context.DisplayName);

        return new ValidationError(attribute, value, message == null ? result : new ValidationResult(message, result.MemberNames));
    }

    private sealed class ValidationError(ValidationAttribute attribute, object value, ValidationResult result)
    {
        public ValidationAttribute Attribute { get; } = attribute;

        public ValidationResult Result { get; } = result;

        public object Value { get; } = value;
    }
}
