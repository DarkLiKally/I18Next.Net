using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.DataAnnotations.Tests;

public class I18NextValidationLocalizerFixture
{
    public I18NextValidationLocalizerFixture()
    {
        _backend = new InMemoryBackend();

        _backend.AddTranslation("en", "validation", "custom", "{{- field}} is custom.");
        _backend.AddTranslation("en", "validation", "even", "{{- field}} must be divisible by {{divisor}}.");
        _backend.AddTranslation("en", "other", "custom", "{{- field}} is from another namespace.");
        _backend.AddTranslation("en", "translation", "Email", "E-mail address");
        _backend.AddTranslation("en", "fields", "Email", "Your e-mail address");
        _backend.AddTranslation("de", "translation", "Email", "E-Mail-Adresse");
        _backend.AddTranslation("de", "translation", "ConfirmEmail", "E-Mail-Bestätigung");
        _backend.AddTranslation("fr", "validation", "required", "Le champ {{- field}} est obligatoire.");

        _i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend)) { Language = "en" };
        _localizer = new I18NextValidationLocalizer(_i18Next);
    }

    private readonly InMemoryBackend _backend;
    private readonly I18NextNet _i18Next;
    private readonly I18NextValidationLocalizer _localizer;

    public static TheoryData<ValidationAttribute> BuiltInAttributes => new()
    {
        new RequiredAttribute(),
        new StringLengthAttribute(10),
        new StringLengthAttribute(10) { MinimumLength = 2 },
        new RangeAttribute(1, 10),
        new RangeAttribute(typeof(DateTime), "2000-01-01", "2010-12-31"),
        new RangeAttribute(1, 10) { MinimumIsExclusive = true },
        new RangeAttribute(1, 10) { MaximumIsExclusive = true },
        new RangeAttribute(1, 10) { MinimumIsExclusive = true, MaximumIsExclusive = true },
        new MinLengthAttribute(3),
        new MaxLengthAttribute(5),
        new LengthAttribute(2, 5),
        new RegularExpressionAttribute("^[a-z]+/[0-9]$"),
        new CompareAttribute("Other"),
        new EmailAddressAttribute(),
        new PhoneAttribute(),
        new UrlAttribute(),
        new CreditCardAttribute(),
        new FileExtensionsAttribute(),
        new FileExtensionsAttribute { Extensions = "PDF, .docx" },
        new EnumDataTypeAttribute(typeof(DayOfWeek)),
        new AllowedValuesAttribute("a", "b"),
        new DeniedValuesAttribute("a", "b"),
        new Base64StringAttribute()
    };

    [Theory]
    [MemberData(nameof(BuiltInAttributes))]
    public void GetErrorMessage_BuiltInAttribute_ShouldMatchDotNetMessage(ValidationAttribute attribute)
    {
        var expected = attribute.FormatErrorMessage("Name");

        _localizer.GetErrorMessage(attribute, "Name").ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(BuiltInAttributes))]
    public void GetErrorMessage_BuiltInAttributeInGerman_ShouldUseGermanDefault(ValidationAttribute attribute)
    {
        var message = _localizer.GetErrorMessage(attribute, "Name", "de");

        message.ShouldContain("Name");
        message.ShouldNotBe(attribute.FormatErrorMessage("Name"));
    }

    [Fact]
    public void GetErrorMessage_German_ShouldUseGermanDefault()
    {
        _localizer.GetErrorMessage(new RequiredAttribute(), "Name", "de").ShouldBe("Das Feld Name ist erforderlich.");
        _localizer.GetErrorMessage(new StringLengthAttribute(10) { MinimumLength = 2 }, "Name", "de")
            .ShouldBe("Das Feld Name muss eine Zeichenfolge mit einer minimalen Länge von 2 und einer maximalen Länge von 10 sein.");
    }

    [Fact]
    public void GetErrorMessage_RegionalLanguage_ShouldUseDefaultOfLanguagePart()
    {
        _localizer.GetErrorMessage(new RequiredAttribute(), "Name", "de-CH").ShouldBe("Das Feld Name ist erforderlich.");
    }

    [Fact]
    public void GetErrorMessage_LanguageWithoutDefaults_ShouldUseEnglishDefault()
    {
        _localizer.GetErrorMessage(new RequiredAttribute(), "Name", "it").ShouldBe("The Name field is required.");
    }

    [Fact]
    public void GetErrorMessage_Translation_ShouldOverrideDefault()
    {
        _localizer.GetErrorMessage(new RequiredAttribute(), "Nom", "fr").ShouldBe("Le champ Nom est obligatoire.");
    }

    [Fact]
    public void GetErrorMessage_WithoutLanguage_ShouldUseCurrentLanguage()
    {
        _i18Next.Language = "fr";

        _localizer.GetErrorMessage(new RequiredAttribute(), "Nom").ShouldBe("Le champ Nom est obligatoire.");
    }

    [Fact]
    public void GetErrorMessage_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        var i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend), new DefaultLanguageDetector("de"))
        {
            Language = "en",
            DetectLanguageOnEachTranslation = true
        };

        new I18NextValidationLocalizer(i18Next).GetErrorMessage(new RequiredAttribute(), "Name").ShouldBe("Das Feld Name ist erforderlich.");
    }

    [Fact]
    public void GetErrorMessage_DefaultMessagesDisabled_ShouldReturnNullForMissingTranslation()
    {
        var localizer = new I18NextValidationLocalizer(_i18Next, new I18NextDataAnnotationsOptions { UseDefaultMessages = false });

        localizer.GetErrorMessage(new RequiredAttribute(), "Name").ShouldBeNull();
        localizer.GetErrorMessage(new RequiredAttribute(), "Nom", "fr").ShouldBe("Le champ Nom est obligatoire.");
    }

    [Fact]
    public void GetErrorMessage_CustomNamespace_ShouldUseNamespace()
    {
        var localizer = new I18NextValidationLocalizer(_i18Next, new I18NextDataAnnotationsOptions { Namespace = "other" });

        localizer.GetErrorMessage(new RequiredAttribute { ErrorMessage = "custom" }, "Name").ShouldBe("Name is from another namespace.");
        localizer.GetErrorMessage(new RequiredAttribute(), "Name").ShouldBe("The Name field is required.");
    }

    [Fact]
    public void GetErrorMessage_ErrorMessageKey_ShouldTranslate()
    {
        _localizer.GetErrorMessage(new RequiredAttribute { ErrorMessage = "custom" }, "Name").ShouldBe("Name is custom.");
    }

    [Fact]
    public void GetErrorMessage_ErrorMessageKeyOnAttributeWithDefaultMessage_ShouldTranslate()
    {
        _localizer.GetErrorMessage(new EmailAddressAttribute { ErrorMessage = "custom" }, "Name").ShouldBe("Name is custom.");
        _localizer.GetErrorMessage(new EmailAddressAttribute { ErrorMessage = "The {0} is not an e-mail address." }, "Name").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_ErrorMessageKeyWithNamespace_ShouldTranslate()
    {
        _localizer.GetErrorMessage(new RequiredAttribute { ErrorMessage = "other:custom" }, "Name").ShouldBe("Name is from another namespace.");
    }

    [Fact]
    public void GetErrorMessage_ErrorMessageWithoutTranslation_ShouldReturnNull()
    {
        _localizer.GetErrorMessage(new RequiredAttribute { ErrorMessage = "Please enter the {0}." }, "Name").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_ErrorMessagesAsKeysDisabled_ShouldReturnNull()
    {
        var localizer = new I18NextValidationLocalizer(_i18Next, new I18NextDataAnnotationsOptions { UseErrorMessagesAsKeys = false });
        var attribute = new RequiredAttribute { ErrorMessage = "custom" };

        localizer.CanTranslate(attribute).ShouldBeFalse();
        localizer.GetErrorMessage(attribute, "Name").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_ResourceMessage_ShouldReturnNull()
    {
        var attribute = new RequiredAttribute { ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.Required) };

        _localizer.CanTranslate(attribute).ShouldBeFalse();
        _localizer.GetErrorMessage(attribute, "Name").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_UnknownAttribute_ShouldReturnNull()
    {
        var attribute = new EvenAttribute();

        _localizer.CanTranslate(attribute).ShouldBeFalse();
        _localizer.GetErrorMessage(attribute, "Name").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_MappedAttribute_ShouldTranslateWithArguments()
    {
        var options = new I18NextDataAnnotationsOptions().MapAttribute<EvenAttribute>("even", a => new { divisor = a.Divisor });
        var localizer = new I18NextValidationLocalizer(_i18Next, options);

        localizer.GetErrorMessage(new EvenAttribute(), "Count").ShouldBe("Count must be divisible by 2.");
    }

    [Fact]
    public void GetErrorMessage_MappedAttributeWithoutTranslation_ShouldReturnNull()
    {
        var options = new I18NextDataAnnotationsOptions().MapAttribute<EvenAttribute>("missing");
        var localizer = new I18NextValidationLocalizer(_i18Next, options);

        localizer.CanTranslate(new EvenAttribute()).ShouldBeTrue();
        localizer.GetErrorMessage(new EvenAttribute(), "Count").ShouldBeNull();
    }

    [Fact]
    public void GetErrorMessage_MappedBuiltInAttribute_ShouldUseNewKey()
    {
        var options = new I18NextDataAnnotationsOptions().MapAttribute<RequiredAttribute>("custom");

        new I18NextValidationLocalizer(_i18Next, options).GetErrorMessage(new RequiredAttribute(), "Name").ShouldBe("Name is custom.");
    }

    [Fact]
    public void GetErrorMessage_DerivedAttribute_ShouldUseBaseMapping()
    {
        _localizer.GetErrorMessage(new DerivedRequiredAttribute(), "Name", "de").ShouldBe("Das Feld Name ist erforderlich.");
    }

    [Fact]
    public void GetErrorMessage_Compare_ShouldTranslateOtherDisplayName()
    {
        _localizer.GetErrorMessage(new CompareAttribute("ConfirmEmail"), "E-Mail-Adresse", "de")
            .ShouldBe("'E-Mail-Adresse' und 'E-Mail-Bestätigung' stimmen nicht überein.");
    }

    [Fact]
    public void GetErrorMessage_HtmlInterpolator_ShouldNotEscapeValues()
    {
        var i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend, new HtmlInterpolator(new TraceLogger()))) { Language = "en" };
        var localizer = new I18NextValidationLocalizer(i18Next);

        localizer.GetErrorMessage(new RegularExpressionAttribute("^<a>/'b'$"), "Name & Title")
            .ShouldBe("The field Name & Title must match the regular expression '^<a>/'b'$'.");
    }

    [Fact]
    public void GetErrorMessage_NullAttribute_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _localizer.GetErrorMessage(null, "Name"));
    }

    [Fact]
    public void GetDisplayName_ExistingTranslation_ShouldTranslate()
    {
        _localizer.GetDisplayName("Email").ShouldBe("E-mail address");
        _localizer.GetDisplayName("Email", "de").ShouldBe("E-Mail-Adresse");
    }

    [Fact]
    public void GetDisplayName_MissingTranslation_ShouldReturnName()
    {
        _localizer.GetDisplayName("Phone number").ShouldBe("Phone number");
        _localizer.GetDisplayName("Note: name").ShouldBe("Note: name");
    }

    [Fact]
    public void GetDisplayName_NullOrEmpty_ShouldReturnName()
    {
        _localizer.GetDisplayName(null).ShouldBeNull();
        _localizer.GetDisplayName(string.Empty).ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDisplayName_Disabled_ShouldReturnName()
    {
        var localizer = new I18NextValidationLocalizer(_i18Next, new I18NextDataAnnotationsOptions { TranslateDisplayNames = false });

        localizer.GetDisplayName("Email").ShouldBe("Email");
    }

    [Fact]
    public void GetDisplayName_DisplayNameNamespace_ShouldUseNamespace()
    {
        var localizer = new I18NextValidationLocalizer(_i18Next, new I18NextDataAnnotationsOptions { DisplayNameNamespace = "fields" });

        localizer.GetDisplayName("Email").ShouldBe("Your e-mail address");
        localizer.GetDisplayName("translation:Email").ShouldBe("E-mail address");
    }

    [Fact]
    public void GetDisplayName_InvalidKey_ShouldReturnName()
    {
        var i18Next = Substitute.For<II18Next>();
        i18Next.Language.Returns("en");
        i18Next.ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<bool>(new TranslationKeyInvalidException("Email.Primary")));

        new I18NextValidationLocalizer(i18Next).GetDisplayName("Email.Primary").ShouldBe("Email.Primary");
    }

    [Fact]
    public void Constructor_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new I18NextValidationLocalizer(null));
        Should.Throw<ArgumentNullException>(() => new I18NextValidationLocalizer(_i18Next, null));
    }

    [Fact]
    public void Options_InvalidValues_ShouldThrow()
    {
        var options = new I18NextDataAnnotationsOptions();

        Should.Throw<ArgumentException>(() => options.Namespace = string.Empty);
        Should.Throw<ArgumentException>(() => options.MapAttribute<EvenAttribute>(null));
    }

    public static class Messages
    {
        public static string Required => "{0} from resources.";
    }

    private sealed class DerivedRequiredAttribute : RequiredAttribute;

    private sealed class EvenAttribute() : ValidationAttribute("The field {0} must be even.")
    {
        public int Divisor => 2;

        public override bool IsValid(object value)
        {
            return value is not int number || number % Divisor == 0;
        }
    }
}
