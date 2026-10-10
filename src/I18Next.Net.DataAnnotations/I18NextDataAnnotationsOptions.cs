using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

using I18Next.Net.Internal;

namespace I18Next.Net.DataAnnotations;

/// <summary>
///     Options of the DataAnnotations integration of I18Next.
/// </summary>
public class I18NextDataAnnotationsOptions
{
    private readonly Dictionary<Type, ValidationAttributeMapping> _mappings = new()
    {
        [typeof(RequiredAttribute)] = Map("required"),
        [typeof(StringLengthAttribute)] = Map<StringLengthAttribute>(a => a.MinimumLength != 0 ? "stringLengthIncludingMinimum" : "stringLength",
            (a, args) =>
            {
                args["min"] = a.MinimumLength;
                args["max"] = a.MaximumLength;
            }),
        [typeof(RangeAttribute)] = Map<RangeAttribute>(GetRangeKey, (a, args) =>
        {
            args["min"] = a.Minimum;
            args["max"] = a.Maximum;
        }),
        [typeof(MinLengthAttribute)] = Map<MinLengthAttribute>(_ => "minLength", (a, args) => args["length"] = a.Length),
        [typeof(MaxLengthAttribute)] = Map<MaxLengthAttribute>(_ => "maxLength", (a, args) => args["length"] = a.Length),
        [typeof(RegularExpressionAttribute)] = Map<RegularExpressionAttribute>(_ => "regularExpression", (a, args) => args["pattern"] = a.Pattern),
        [typeof(CompareAttribute)] = Map("compare"),
        [typeof(EmailAddressAttribute)] = Map("emailAddress"),
        [typeof(PhoneAttribute)] = Map("phone"),
        [typeof(UrlAttribute)] = Map("url"),
        [typeof(CreditCardAttribute)] = Map("creditCard"),
        [typeof(FileExtensionsAttribute)] = Map<FileExtensionsAttribute>(_ => "fileExtensions", (a, args) => args["extensions"] = FormatExtensions(a.Extensions)),
        [typeof(EnumDataTypeAttribute)] = Map("enumDataType"),
#if NET8_0_OR_GREATER
        [typeof(LengthAttribute)] = Map<LengthAttribute>(_ => "length", (a, args) =>
        {
            args["min"] = a.MinimumLength;
            args["max"] = a.MaximumLength;
        }),
        [typeof(AllowedValuesAttribute)] = Map<AllowedValuesAttribute>(_ => "allowedValues", (a, args) => args["values"] = FormatValues(a.Values)),
        [typeof(DeniedValuesAttribute)] = Map<DeniedValuesAttribute>(_ => "deniedValues", (a, args) => args["values"] = FormatValues(a.Values)),
        [typeof(Base64StringAttribute)] = Map("base64String"),
#endif
    };

    /// <summary>
    ///     The namespace containing the validation messages. Defaults to <c>validation</c>.
    /// </summary>
    public string Namespace
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? throw new ArgumentException("Namespace cannot be null or empty.", nameof(value)) : value;
    } = "validation";

    /// <summary>
    ///     Uses the built-in English and German messages if a validation message is missing in the namespace.
    ///     Otherwise the untranslated message of the attribute is used. Defaults to <c>true</c>.
    /// </summary>
    public bool UseDefaultMessages { get; set; } = true;

    /// <summary>
    ///     Uses the <see cref="ValidationAttribute.ErrorMessage" /> set on an attribute as a key in the validation namespace.
    ///     The message is used as it is if no translation exists for it. Defaults to <c>true</c>.
    /// </summary>
    public bool UseErrorMessagesAsKeys { get; set; } = true;

    /// <summary>
    ///     Translates the display names. The name of the <see cref="DisplayAttribute" />, the
    ///     <see cref="System.ComponentModel.DisplayNameAttribute" /> or the name of the property is used as the key and kept if
    ///     no translation exists. Defaults to <c>true</c>.
    /// </summary>
    public bool TranslateDisplayNames { get; set; } = true;

    /// <summary>
    ///     The namespace containing the display names. Uses the default namespace of I18Next if not set.
    /// </summary>
    public string DisplayNameNamespace { get; set; }

    /// <summary>
    ///     Translates the model binding messages of ASP.NET Core MVC, like "The value '{0}' is not valid for {1}.". Defaults
    ///     to <c>true</c>.
    /// </summary>
    public bool TranslateModelBindingMessages { get; set; } = true;

    /// <summary>
    ///     Translates the default error message of a custom validation attribute with the given key of the validation
    ///     namespace. Attributes deriving from the given type use the mapping too.
    /// </summary>
    /// <param name="key">The key of the error message in the validation namespace.</param>
    /// <param name="arguments">Provides additional interpolation arguments from the attribute.</param>
    /// <typeparam name="TAttribute">The type of the validation attribute.</typeparam>
    /// <returns>The current options instance.</returns>
    /// <exception cref="ArgumentException">If the provided key is null or empty.</exception>
    public I18NextDataAnnotationsOptions MapAttribute<TAttribute>(string key, Func<TAttribute, object> arguments = null)
        where TAttribute : ValidationAttribute
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        _mappings[typeof(TAttribute)] = Map<TAttribute>(_ => key, arguments == null
            ? null
            : (a, args) =>
            {
                foreach (var argument in arguments(a).ToDictionary())
                    args[argument.Key] = argument.Value;
            });

        return this;
    }

    internal ValidationAttributeMapping FindMapping(Type attributeType)
    {
        for (var type = attributeType; type != null && type != typeof(ValidationAttribute); type = type.BaseType)
        {
            if (_mappings.TryGetValue(type, out var mapping))
                return mapping;
        }

        return null;
    }

    private static ValidationAttributeMapping Map(string key)
    {
        return new ValidationAttributeMapping(_ => key, null);
    }

    private static ValidationAttributeMapping Map<TAttribute>(Func<TAttribute, string> key, Action<TAttribute, IDictionary<string, object>> addArguments)
        where TAttribute : ValidationAttribute
    {
        return new ValidationAttributeMapping(a => key((TAttribute)a), addArguments == null ? null : (a, args) => addArguments((TAttribute)a, args));
    }

    private static string FormatExtensions(string extensions)
    {
        return string.Join(", ", extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant().Split(',').Select(e => "." + e));
    }

    private static string FormatValues(IEnumerable<object> values)
    {
        return string.Join(", ", values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)));
    }

    private static string GetRangeKey(RangeAttribute attribute)
    {
#if NET8_0_OR_GREATER
        if (attribute.MinimumIsExclusive)
            return attribute.MaximumIsExclusive ? "rangeMinMaxExclusive" : "rangeMinExclusive";

        if (attribute.MaximumIsExclusive)
            return "rangeMaxExclusive";
#endif

        return "range";
    }
}
