using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.DataAnnotations.Tests;

public class I18NextValidatorFixture
{
    public I18NextValidatorFixture()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("de", "translation", "Name", "Vorname");
        backend.AddTranslation("de", "translation", "fields.email", "E-Mail-Adresse");
        backend.AddTranslation("de", "translation", "Person", "Die Person");
        backend.AddTranslation("de", "validation", "nameReserved", "{{- field}} ist reserviert.");

        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de" };
        _validator = new I18NextValidator(_i18Next);
    }

    private readonly I18NextNet _i18Next;
    private readonly I18NextValidator _validator;

    [Fact]
    public void TryValidateObject_RequiredProperties_ShouldTranslateMessages()
    {
        var person = new Person();
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(person, new ValidationContext(person), results).ShouldBeFalse();

        results.Select(r => r.ErrorMessage).ShouldBe(["Das Feld Vorname ist erforderlich.", "Das Feld E-Mail-Adresse ist erforderlich."]);
        results[0].MemberNames.ShouldBe(["Name"]);
    }

    [Fact]
    public void TryValidateObject_RequiredPropertiesOnly_ShouldSkipOtherAttributes()
    {
        var person = new Person { Name = "Jo", Email = "invalid", Age = 5 };

        _validator.TryValidateObject(person, new ValidationContext(person), new List<ValidationResult>()).ShouldBeTrue();
    }

    [Fact]
    public void TryValidateObject_AllProperties_ShouldTranslateMessages()
    {
        var person = new Person { Name = "Jo", Email = "invalid", Age = 5 };
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(person, new ValidationContext(person), results, true).ShouldBeFalse();

        results.Select(r => r.ErrorMessage).ShouldBe([
            "Das Feld Vorname muss eine Zeichenfolge mit einer minimalen Länge von 3 und einer maximalen Länge von 20 sein.",
            "Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse.",
            "Das Feld Age muss zwischen 18 und 120 liegen."
        ]);
    }

    [Fact]
    public void TryValidateObject_WithoutResults_ShouldStopAtFirstError()
    {
        var person = new Person();

        _validator.TryValidateObject(person, new ValidationContext(person), null, true).ShouldBeFalse();
    }

    [Fact]
    public void TryValidateObject_ValidProperties_ShouldValidateTypeAttributes()
    {
        var person = new Person { Name = "Admin", Email = "admin@example.com" };
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(person, new ValidationContext(person), results).ShouldBeFalse();

        results.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Die Person ist reserviert.");
    }

    [Fact]
    public void TryValidateObject_ValidAttributes_ShouldCallValidatableObject()
    {
        var person = new Person { Name = "Jane", Email = "jane@example.com", Age = 150 };
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(person, new ValidationContext(person), results).ShouldBeFalse();

        results.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Too old.");
    }

    [Fact]
    public void TryValidateObject_ValidObject_ShouldSucceed()
    {
        var person = new Person { Name = "Jane", Email = "jane@example.com", Age = 30 };
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(person, new ValidationContext(person), results, true).ShouldBeTrue();

        results.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateObject_UntranslatedAttribute_ShouldUseTranslatedDisplayName()
    {
        var item = new Item { Name = "x" };
        var results = new List<ValidationResult>();

        _validator.TryValidateObject(item, new ValidationContext(item), results, true).ShouldBeFalse();

        results.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Vorname is invalid.");
    }

    [Fact]
    public void TryValidateObject_InvalidArguments_ShouldThrow()
    {
        var person = new Person();

        Should.Throw<ArgumentNullException>(() => _validator.TryValidateObject(null, new ValidationContext(person), null));
        Should.Throw<ArgumentNullException>(() => _validator.TryValidateObject(person, null, null));
        Should.Throw<ArgumentException>(() => _validator.TryValidateObject(person, new ValidationContext(new Person()), null));
    }

    [Fact]
    public void TryValidateProperty_InvalidValue_ShouldTranslateMessages()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(new Person()) { MemberName = nameof(Person.Name) };

        _validator.TryValidateProperty("Jo", context, results).ShouldBeFalse();

        results.ShouldHaveSingleItem().ErrorMessage
            .ShouldBe("Das Feld Vorname muss eine Zeichenfolge mit einer minimalen Länge von 3 und einer maximalen Länge von 20 sein.");
    }

    [Fact]
    public void TryValidateProperty_MissingRequiredValue_ShouldOnlyReportRequired()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(new Person()) { MemberName = nameof(Person.Name) };

        _validator.TryValidateProperty(null, context, results).ShouldBeFalse();

        results.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Das Feld Vorname ist erforderlich.");
    }

    [Fact]
    public void TryValidateProperty_InvalidArguments_ShouldThrow()
    {
        var person = new Person();

        Should.Throw<ArgumentNullException>(() => _validator.TryValidateProperty("Jane", null, null));
        Should.Throw<ArgumentException>(() => _validator.TryValidateProperty("Jane", new ValidationContext(person), null));
        Should.Throw<ArgumentException>(() => _validator.TryValidateProperty("Jane", new ValidationContext(person) { MemberName = "Unknown" }, null));
        Should.Throw<ArgumentException>(() => _validator.TryValidateProperty(5, new ValidationContext(person) { MemberName = nameof(Person.Name) }, null));
        Should.Throw<ArgumentException>(() => _validator.TryValidateProperty(null, new ValidationContext(person) { MemberName = nameof(Person.Count) }, null));
    }

    [Fact]
    public void TryValidateProperty_NullForNullableValueType_ShouldSucceed()
    {
        var context = new ValidationContext(new Person()) { MemberName = nameof(Person.Age) };

        _validator.TryValidateProperty(null, context, null).ShouldBeTrue();
    }

    [Fact]
    public void TryValidateValue_ExplicitDisplayName_ShouldTranslateDisplayName()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(new object()) { DisplayName = "fields.email" };

        _validator.TryValidateValue("invalid", context, results, [new EmailAddressAttribute(), new StringLengthAttribute(3)]).ShouldBeFalse();

        results.Select(r => r.ErrorMessage).ShouldBe([
            "Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse.",
            "Das Feld E-Mail-Adresse muss eine Zeichenfolge mit einer maximalen Länge von 3 sein."
        ]);
    }

    [Fact]
    public void TryValidateValue_InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _validator.TryValidateValue("x", null, null, []));
        Should.Throw<ArgumentNullException>(() => _validator.TryValidateValue("x", new ValidationContext(new object()), null, null));
    }

    [Fact]
    public void ValidateObject_InvalidObject_ShouldThrowTranslatedException()
    {
        var person = new Person();

        var exception = Should.Throw<ValidationException>(() => _validator.ValidateObject(person, new ValidationContext(person)));

        exception.Message.ShouldBe("Das Feld Vorname ist erforderlich.");
        exception.ValidationAttribute.ShouldBeOfType<RequiredAttribute>();
        exception.Value.ShouldBeNull();
    }

    [Fact]
    public void ValidateObject_ValidObject_ShouldNotThrow()
    {
        var person = new Person { Name = "Jane", Email = "jane@example.com" };

        Should.NotThrow(() => _validator.ValidateObject(person, new ValidationContext(person), true));
    }

    [Fact]
    public void ValidateProperty_InvalidValue_ShouldThrowTranslatedException()
    {
        var context = new ValidationContext(new Person()) { MemberName = nameof(Person.Email) };

        var exception = Should.Throw<ValidationException>(() => _validator.ValidateProperty("invalid", context));

        exception.Message.ShouldBe("Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse.");
        exception.Value.ShouldBe("invalid");
    }

    [Fact]
    public void ValidateValue_InvalidValue_ShouldThrowTranslatedException()
    {
        var context = new ValidationContext(new object()) { DisplayName = "Name" };

        var exception = Should.Throw<ValidationException>(() => _validator.ValidateValue(null, context, [new RequiredAttribute()]));

        exception.Message.ShouldBe("Das Feld Vorname ist erforderlich.");
        Should.Throw<ArgumentNullException>(() => _validator.ValidateValue(null, null, []));
        Should.Throw<ArgumentNullException>(() => _validator.ValidateValue(null, context, null));
    }

    [Fact]
    public void Constructor_NullLocalizer_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new I18NextValidator((I18NextValidationLocalizer)null));
    }

    [ReservedName]
    public class Person : IValidatableObject
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "fields.email")]
        public string Email { get; set; }

        [Range(18, 120)]
        public int? Age { get; set; }

        [Range(0, 10)]
        public int Count { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Age > 120)
                yield return new ValidationResult("Too old.", [nameof(Age)]);
        }
    }

    public class Item
    {
        [MinLength(2, ErrorMessage = "{0} is invalid.")]
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class ReservedNameAttribute : ValidationAttribute
    {
        public ReservedNameAttribute()
        {
            ErrorMessage = "nameReserved";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value is Person { Name: "Admin" } ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName)) : ValidationResult.Success;
        }
    }
}
