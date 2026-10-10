# Validation messages

`I18Next.Net.DataAnnotations` translates the messages of the DataAnnotations attributes in ASP.NET Core MVC, minimal
APIs and the `Validator`, `I18Next.Net.FluentValidation` the messages of FluentValidation. The messages are looked up in
the `validation` namespace and the display names in the default namespace, so the attributes don't need an
`ErrorMessage`.

## ASP.NET Core MVC

```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddBackend(new JsonFileBackend("locales"))
    .UseDefaultLanguage("en"));

services.AddControllers()
    .AddI18NextDataAnnotationsLocalization();

app.UseRequestLocalization(options => options.AddSupportedCultures("de", "en"));
```

```csharp
public class Customer
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "fields.email")]
    public string Email { get; set; }

    [RegularExpression("^[A-Z]{2}[0-9]{4}$", ErrorMessage = "customerNumber")]
    public string CustomerNumber { get; set; }
}
```

```json
// locales/de/translation.json
{
    "Name": "Name",
    "fields": {
        "email": "E-Mail-Adresse"
    }
}

// locales/de/validation.json
{
    "required": "Bitte geben Sie {{- field}} an.",
    "customerNumber": "{{- field}} muss aus zwei Großbuchstaben und vier Ziffern bestehen."
}
```

```json
{
    "errors": {
        "Name": [ "Bitte geben Sie Name an." ],
        "Email": [ "Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse." ],
        "CustomerNumber": [ "CustomerNumber muss aus zwei Großbuchstaben und vier Ziffern bestehen." ]
    }
}
```

The server side validation, the `data-val-*` attributes of the client side validation, the display names of labels and
the model binding messages like "The value 'abc' is not valid for Age." are translated. English and German messages are
built in, so only the messages you want to change or other languages need a `validation.json`. A key found in a
fallback language of I18Next is used before the built-in message of the requested language.

| Translated text | Key |
|---|---|
| Message of a validation attribute without `ErrorMessage` | `validation:{key}` of the table below |
| `ErrorMessage` of an attribute | `validation:{ErrorMessage}`, or the message itself if the key doesn't exist |
| Display name | `DisplayAttribute.Name`, `DisplayNameAttribute.DisplayName` or the property name, kept if the key doesn't exist |
| Model binding message | `validation:modelBinding.{key}` |

Attributes using `ErrorMessageResourceType` and display names using `ResourceType` keep their resources. The
placeholders are written as `{{- field}}` because the `HtmlInterpolator` of `IntegrateToAspNetCore` escapes the values
of `{{field}}`, while MVC encodes the messages itself.

## Minimal APIs

On .NET 10 the validation of minimal APIs is translated when the DataAnnotations integration is added to I18Next:

```csharp
builder.Services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddDataAnnotationsLocalization());

builder.Services.AddValidation();

app.MapPost("/customers", (Customer customer) => Results.Ok(customer));
```

`Microsoft.Extensions.Validation` has no localization hook in .NET 10. The integration wraps the validated endpoint
parameters and replaces the messages reported by the validation attributes. Messages of `IValidatableObject` and other
validations of `ValidationOptions` used without an endpoint, e.g. by Blazor, are not translated.

## Without ASP.NET Core

`I18NextValidator` validates like the `Validator` of `System.ComponentModel.DataAnnotations` and translates the results.
On .NET 6 and later the package references the ASP.NET Core shared framework, the `netstandard2.0` build doesn't.

```csharp
var validator = new I18NextValidator(i18n);
var results = new List<ValidationResult>();

validator.TryValidateObject(customer, new ValidationContext(customer), results, validateAllProperties: true);
```

With dependency injection `AddDataAnnotationsLocalization` registers the `I18NextValidator` and the
`I18NextValidationLocalizer`, which translates single attributes and display names:

```csharp
var localizer = new I18NextValidationLocalizer(i18n);

localizer.GetDisplayName("fields.email");                          // E-Mail-Adresse
localizer.GetErrorMessage(new RequiredAttribute(), "E-Mail-Adresse"); // Das Feld E-Mail-Adresse ist erforderlich.
```

## Options

```csharp
services.AddControllers().AddI18NextDataAnnotationsLocalization(options =>
{
    options.Namespace = "messages";
    options.DisplayNameNamespace = "fields";
    options.MapAttribute<EvenAttribute>("even", a => new { divisor = a.Divisor });
});
```

| Option | Default | Description |
|---|---|---|
| `Namespace` | `validation` | Namespace of the messages |
| `UseDefaultMessages` | `true` | Uses the built-in English and German messages for missing keys, otherwise the untranslated message of the attribute |
| `UseErrorMessagesAsKeys` | `true` | Uses the `ErrorMessage` of an attribute as key |
| `TranslateDisplayNames` | `true` | Translates the display names |
| `DisplayNameNamespace` | | Namespace of the display names, the default namespace if not set |
| `TranslateModelBindingMessages` | `true` | Translates the model binding messages of MVC |
| `MapAttribute<T>(key, arguments)` | | Translates a custom validation attribute and the attributes deriving from it |

## Keys

The default files can be copied from
[`src/I18Next.Net.DataAnnotations/Resources`](https://github.com/DarkLiKally/I18Next.Net/tree/develop/src/I18Next.Net.DataAnnotations/Resources).
Every message has the `field` placeholder.

| Key | Attribute | Placeholders |
|---|---|---|
| `required` | `Required` | |
| `stringLength` | `StringLength` | `max`, `min` |
| `stringLengthIncludingMinimum` | `StringLength` with `MinimumLength` | `max`, `min` |
| `range` | `Range` | `min`, `max` |
| `rangeMinExclusive` | `Range` with `MinimumIsExclusive` | `min`, `max` |
| `rangeMaxExclusive` | `Range` with `MaximumIsExclusive` | `min`, `max` |
| `rangeMinMaxExclusive` | `Range` with both exclusive | `min`, `max` |
| `minLength` | `MinLength` | `length` |
| `maxLength` | `MaxLength` | `length` |
| `length` | `Length` | `min`, `max` |
| `regularExpression` | `RegularExpression` | `pattern` |
| `compare` | `Compare` | `other` |
| `emailAddress` | `EmailAddress` | |
| `phone` | `Phone` | |
| `url` | `Url` | |
| `creditCard` | `CreditCard` | |
| `fileExtensions` | `FileExtensions` | `extensions` |
| `enumDataType` | `EnumDataType` | |
| `allowedValues` | `AllowedValues` | `values` |
| `deniedValues` | `DeniedValues` | `values` |
| `base64String` | `Base64String` | |

| Key | Placeholders | English message |
|---|---|---|
| `modelBinding.attemptedValueIsInvalid` | `value`, `field` | `The value '{{- value}}' is not valid for {{- field}}.` |
| `modelBinding.missingBindRequiredValue` | `field` | `A value for the '{{- field}}' parameter or property was not provided.` |
| `modelBinding.missingKeyOrValue` | | `A value is required.` |
| `modelBinding.missingRequestBodyRequiredValue` | | `A non-empty request body is required.` |
| `modelBinding.nonPropertyAttemptedValueIsInvalid` | `value` | `The value '{{- value}}' is not valid.` |
| `modelBinding.nonPropertyUnknownValueIsInvalid` | | `The supplied value is invalid.` |
| `modelBinding.nonPropertyValueMustBeANumber` | | `The field must be a number.` |
| `modelBinding.unknownValueIsInvalid` | `field` | `The supplied value is invalid for {{- field}}.` |
| `modelBinding.valueIsInvalid` | `value` | `The value '{{- value}}' is invalid.` |
| `modelBinding.valueMustBeANumber` | `field` | `The field {{- field}} must be a number.` |
| `modelBinding.valueMustNotBeNull` | `value` | `The value '{{- value}}' is invalid.` |

## FluentValidation

```csharp
services.AddI18NextLocalization(i18n => i18n
    .IntegrateToAspNetCore()
    .AddFluentValidationLocalization());
```

`AddFluentValidationLocalization` registers the `I18NextLanguageManager` and the `I18NextDisplayNameResolver` and assigns
them to `ValidatorOptions.Global` when the host starts. Without a host they are assigned directly:

```csharp
ValidatorOptions.Global.LanguageManager = new I18NextLanguageManager(i18n);
ValidatorOptions.Global.DisplayNameResolver = new I18NextDisplayNameResolver(i18n).Resolve;
```

The message keys of FluentValidation, e.g. `NotEmptyValidator`, and the error codes set with `WithErrorCode` are looked up
in the `validation` namespace. The placeholders of FluentValidation are written with i18next delimiters and converted
back, the built-in message of FluentValidation is used if a key is missing:

```json
// locales/de/validation.json
{
    "NotEmptyValidator": "Bitte geben Sie '{{PropertyName}}' an.",
    "LengthValidator": "'{{PropertyName}}' muss zwischen {{MinLength}} und {{MaxLength}} Zeichen lang sein."
}
```

The English and German messages of FluentValidation can be copied from
[`src/I18Next.Net.FluentValidation/Resources`](https://github.com/DarkLiKally/I18Next.Net/tree/develop/src/I18Next.Net.FluentValidation/Resources).
The language is taken from `ILanguageManager.Culture`, the culture passed by FluentValidation or the current language
of I18Next. The property names are translated like the display names of the DataAnnotations, a resolver assigned before
is used for missing translations and messages set with `WithMessage` are not changed.

| Option | Default | Description |
|---|---|---|
| `Namespace` | `validation` | Namespace of the messages |
| `KeyPrefix` | | Prefix of the keys, e.g. `fluent` for `fluent.NotEmptyValidator` |
| `TranslateDisplayNames` | `true` | Translates the property names |
| `DisplayNameNamespace` | | Namespace of the property names, the default namespace if not set |
