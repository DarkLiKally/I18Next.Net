using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace I18Next.Net.DataAnnotations.Mvc;

internal sealed class I18NextMvcOptionsSetup(I18NextValidationLocalizer localizer) : IConfigureOptions<MvcOptions>
{
    private readonly I18NextValidationLocalizer _localizer = localizer;

    public void Configure(MvcOptions options)
    {
        options.ModelMetadataDetailsProviders.Add(new I18NextDisplayMetadataProvider(_localizer));
        options.ModelValidatorProviders.Insert(0, new I18NextModelValidatorProvider(_localizer));

        if (_localizer.Options.TranslateModelBindingMessages)
            ConfigureModelBindingMessages(options.ModelBindingMessageProvider);
    }

    private void ConfigureModelBindingMessages(DefaultModelBindingMessageProvider provider)
    {
        var defaults = new DefaultModelBindingMessageProvider(provider);

        provider.SetAttemptedValueIsInvalidAccessor((value, field) =>
            Translate("attemptedValueIsInvalid", value, field) ?? defaults.AttemptedValueIsInvalidAccessor(value, field));
        provider.SetMissingBindRequiredValueAccessor(field =>
            Translate("missingBindRequiredValue", null, _localizer.GetDisplayName(field)) ?? defaults.MissingBindRequiredValueAccessor(field));
        provider.SetMissingKeyOrValueAccessor(() =>
            Translate("missingKeyOrValue", null, null) ?? defaults.MissingKeyOrValueAccessor());
        provider.SetMissingRequestBodyRequiredValueAccessor(() =>
            Translate("missingRequestBodyRequiredValue", null, null) ?? defaults.MissingRequestBodyRequiredValueAccessor());
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value =>
            Translate("nonPropertyAttemptedValueIsInvalid", value, null) ?? defaults.NonPropertyAttemptedValueIsInvalidAccessor(value));
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() =>
            Translate("nonPropertyUnknownValueIsInvalid", null, null) ?? defaults.NonPropertyUnknownValueIsInvalidAccessor());
        provider.SetNonPropertyValueMustBeANumberAccessor(() =>
            Translate("nonPropertyValueMustBeANumber", null, null) ?? defaults.NonPropertyValueMustBeANumberAccessor());
        provider.SetUnknownValueIsInvalidAccessor(field =>
            Translate("unknownValueIsInvalid", null, field) ?? defaults.UnknownValueIsInvalidAccessor(field));
        provider.SetValueIsInvalidAccessor(value =>
            Translate("valueIsInvalid", value, null) ?? defaults.ValueIsInvalidAccessor(value));
        provider.SetValueMustBeANumberAccessor(field =>
            Translate("valueMustBeANumber", null, field) ?? defaults.ValueMustBeANumberAccessor(field));
        provider.SetValueMustNotBeNullAccessor(value =>
            Translate("valueMustNotBeNull", value, null) ?? defaults.ValueMustNotBeNullAccessor(value));
    }

    private string Translate(string key, string value, string field)
    {
        return _localizer.GetModelBindingMessage(key, new Dictionary<string, object> { ["value"] = value, ["field"] = field });
    }
}
