using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.DataAnnotations.Mvc;
using I18Next.Net.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.DataAnnotations.Tests;

public class MvcFixture
{
    private static async Task<Dictionary<string, string[]>> GetErrorsAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("errors").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    private static async Task<Dictionary<string, string[]>> SendAsync(HttpRequestMessage request, Action<I18NextDataAnnotationsOptions> configure = null)
    {
        await using var app = await StartAsync(configure);

        return await GetErrorsAsync(await app.GetTestClient().SendAsync(request));
    }

    private static Task<WebApplication> StartAsync(Action<I18NextDataAnnotationsOptions> configure = null)
    {
        return TestApplication.StartAsync(builder => builder.Services
                .AddControllers()
                .AddApplicationPart(typeof(PeopleController).Assembly)
                .AddI18NextDataAnnotationsLocalization(configure),
            app => app.MapControllers());
    }

    private static ServiceProvider CreateServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddI18NextLocalization(i18n => i18n.AddBackend(TestApplication.CreateBackend()).UseDefaultLanguage("de"));

        configure(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Post_InvalidModelInGerman_ShouldReturnTranslatedMessages()
    {
        const string json = """{ "name": "Jo", "email": "invalid", "confirmEmail": "other", "age": 5, "code": "abc" }""";

        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", json));

        errors["Name"].ShouldBe(["Das Feld Vorname muss eine Zeichenfolge mit einer minimalen Länge von 3 und einer maximalen Länge von 20 sein."]);
        errors["Email"].ShouldBe(["Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse."]);
        errors["ConfirmEmail"].ShouldBe(["'E-Mail-Bestätigung' und 'E-Mail-Adresse' stimmen nicht überein."]);
        errors["Age"].ShouldBe(["Das Feld Age muss zwischen 18 und 120 liegen."]);
        errors["Code"].ShouldBe(["Code darf nur Ziffern enthalten."]);
    }

    [Fact]
    public async Task Post_InvalidModelInEnglish_ShouldReturnDefaultMessagesWithTranslatedDisplayNames()
    {
        const string json = """{ "name": "Jo", "email": "invalid", "confirmEmail": "other", "code": "abc" }""";

        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "en", json));

        errors["Name"].ShouldBe(["The field Name must be a string with a minimum length of 3 and a maximum length of 20."]);
        errors["Email"].ShouldBe(["The E-mail/address field is not a valid e-mail address."]);
        errors["ConfirmEmail"].ShouldBe(["'E-mail confirmation' and 'E-mail/address' do not match."]);
        errors["Code"].ShouldBe(["Code may only contain digits."]);
    }

    [Fact]
    public async Task Post_MissingRequiredValues_ShouldReturnTranslatedMessages()
    {
        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", "{}"));

        errors["Name"].ShouldBe(["Das Feld Vorname ist erforderlich."]);
        errors["Email"].ShouldBe(["Das Feld E-Mail-Adresse ist erforderlich."]);
    }

    [Fact]
    public async Task Post_EmptyBody_ShouldReturnTranslatedModelBindingMessage()
    {
        var request = TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", string.Empty);

        var errors = await SendAsync(request);

        errors.Values.SelectMany(e => e).ShouldContain("Ein nicht leerer Anforderungstext ist erforderlich.");
    }

    [Fact]
    public async Task Get_InvalidQueryValue_ShouldReturnTranslatedModelBindingMessage()
    {
        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Get, "/people/age?age=abc", "de"));

        errors["age"].ShouldBe(["Der Wert 'abc' ist für Alter ungültig."]);
    }

    [Fact]
    public async Task Get_ModelBindingMessagesDisabled_ShouldReturnDefaultModelBindingMessage()
    {
        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Get, "/people/age?age=abc", "de"),
            o => o.TranslateModelBindingMessages = false);

        errors["age"].ShouldBe(["The value 'abc' is not valid for Alter."]);
    }

    [Fact]
    public async Task Get_InvalidParameter_ShouldReturnTranslatedMessage()
    {
        var errors = await SendAsync(TestApplication.CreateRequest(HttpMethod.Get, "/people/count?count=20", "de"));

        errors["count"].ShouldBe(["Das Feld Anzahl muss zwischen 1 und 10 liegen."]);
    }

    [Fact]
    public async Task Get_ValidParameter_ShouldSucceed()
    {
        await using var app = await StartAsync();

        var response = await app.GetTestClient().SendAsync(TestApplication.CreateRequest(HttpMethod.Get, "/people/count?count=5", "de"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddValidation_ClientAdapters_ShouldTranslateMessages()
    {
        await using var app = await StartAsync();
        var metadataProvider = app.Services.GetRequiredService<IModelMetadataProvider>();
        var adapterProvider = app.Services.GetRequiredService<IValidationAttributeAdapterProvider>();
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de");

            var required = AddValidation(metadataProvider, adapterProvider, nameof(PersonModel.Email), new RequiredAttribute());
            var compare = AddValidation(metadataProvider, adapterProvider, nameof(PersonModel.ConfirmEmail), new CompareAttribute(nameof(PersonModel.Email)));
            var stringLength = AddValidation(metadataProvider, adapterProvider, nameof(PersonModel.Name), new StringLengthAttribute(20) { MinimumLength = 3 });

            required["data-val-required"].ShouldBe("Das Feld E-Mail-Adresse ist erforderlich.");
            compare["data-val-equalto"].ShouldBe("'E-Mail-Bestätigung' und 'E-Mail-Adresse' stimmen nicht überein.");
            compare["data-val-equalto-other"].ShouldBe("*.Email");
            stringLength["data-val-length"].ShouldBe(
                "Das Feld Vorname muss eine Zeichenfolge mit einer minimalen Länge von 3 und einer maximalen Länge von 20 sein.");
            stringLength["data-val-length-max"].ShouldBe("20");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void GetAttributeAdapter_UntranslatedAttribute_ShouldReturnInnerAdapter()
    {
        var inner = new ValidationAttributeAdapterProvider();
        var provider = new I18NextValidationAttributeAdapterProvider(inner, new I18NextValidationLocalizer(Substitute.For<II18Next>()));
        var attribute = new RequiredAttribute { ErrorMessageResourceType = typeof(I18NextValidationLocalizerFixture.Messages), ErrorMessageResourceName = "Required" };

        provider.GetAttributeAdapter(attribute, null).GetType().ShouldBe(inner.GetAttributeAdapter(attribute, null).GetType());
        provider.GetAttributeAdapter(new CustomValidationAttribute(typeof(object), "Equals"), null).ShouldBeNull();
    }

    [Fact]
    public void AddI18NextDataAnnotationsLocalization_CustomAdapterProvider_ShouldDecorateProvider()
    {
        var inner = Substitute.For<IValidationAttributeAdapterProvider>();

        using var provider = CreateServices(services =>
        {
            services.AddSingleton(inner);
            services.AddControllers().AddI18NextDataAnnotationsLocalization().AddI18NextDataAnnotationsLocalization();
        });

        provider.GetRequiredService<IValidationAttributeAdapterProvider>().ShouldBeOfType<I18NextValidationAttributeAdapterProvider>();
        provider.GetServices<IValidationAttributeAdapterProvider>().Count().ShouldBe(1);
        provider.GetServices<IConfigureOptions<MvcOptions>>().Count(o => o.GetType().Name == "I18NextMvcOptionsSetup").ShouldBe(1);

        provider.GetRequiredService<IValidationAttributeAdapterProvider>().GetAttributeAdapter(new RequiredAttribute(), null);

        inner.Received(1).GetAttributeAdapter(Arg.Any<RequiredAttribute>(), Arg.Any<IStringLocalizer>());
    }

    [Fact]
    public void AddI18NextDataAnnotationsLocalization_FactoryAdapterProvider_ShouldDecorateProvider()
    {
        var inner = Substitute.For<IValidationAttributeAdapterProvider>();

        using var provider = CreateServices(services =>
        {
            services.AddSingleton(_ => inner);
            services.AddControllers().AddI18NextDataAnnotationsLocalization();
        });

        provider.GetRequiredService<IValidationAttributeAdapterProvider>().GetAttributeAdapter(new RequiredAttribute(), null);

        inner.Received(1).GetAttributeAdapter(Arg.Any<RequiredAttribute>(), Arg.Any<IStringLocalizer>());
    }

    [Fact]
    public void AddI18NextDataAnnotationsLocalization_Configure_ShouldApplyOptions()
    {
        using var provider = CreateServices(services => services.AddControllers().AddI18NextDataAnnotationsLocalization(o => o.Namespace = "messages"));

        provider.GetRequiredService<I18NextValidationLocalizer>().Options.Namespace.ShouldBe("messages");
        provider.GetRequiredService<I18NextValidator>().ShouldNotBeNull();
        provider.GetRequiredService<IOptions<MvcOptions>>().Value.ModelValidatorProviders[0].ShouldBeOfType<I18NextModelValidatorProvider>();
        provider.GetRequiredService<IOptions<MvcOptions>>().Value.ModelMetadataDetailsProviders.OfType<I18NextDisplayMetadataProvider>().Count().ShouldBe(1);
    }

    [Fact]
    public void GetDisplayName_DisplayAttributes_ShouldTranslateKeys()
    {
        using var provider = CreateServices(services => services.AddControllers().AddI18NextDataAnnotationsLocalization());
        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();

        string GetDisplayName(string property) => metadataProvider.GetMetadataForProperty(typeof(DisplayModel), property).GetDisplayName();

        GetDisplayName(nameof(DisplayModel.Name)).ShouldBe("Vorname");
        GetDisplayName(nameof(DisplayModel.Email)).ShouldBe("E-Mail-Adresse");
        GetDisplayName(nameof(DisplayModel.City)).ShouldBe("Ort");
        GetDisplayName(nameof(DisplayModel.Order)).ShouldBe("Vorname");
        GetDisplayName(nameof(DisplayModel.Resource)).ShouldBe("{0} from resources.");
        GetDisplayName(nameof(DisplayModel.Missing)).ShouldBe("Missing");
        metadataProvider.GetMetadataForType(typeof(DisplayModel)).GetDisplayName().ShouldBe(nameof(DisplayModel));
    }

    [Fact]
    public void GetDisplayName_TranslationDisabled_ShouldNotTranslate()
    {
        using var provider = CreateServices(services =>
            services.AddControllers().AddI18NextDataAnnotationsLocalization(o => o.TranslateDisplayNames = false));
        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();

        metadataProvider.GetMetadataForProperty(typeof(DisplayModel), nameof(DisplayModel.Name)).GetDisplayName().ShouldBe(nameof(DisplayModel.Name));
        metadataProvider.GetMetadataForProperty(typeof(DisplayModel), nameof(DisplayModel.Email)).GetDisplayName().ShouldBe("fields.email");
    }

    [Fact]
    public void CreateValidators_RequiredAttribute_ShouldBeFirst()
    {
        var provider = new I18NextModelValidatorProvider(new I18NextValidationLocalizer(Substitute.For<II18Next>()));
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));
        var results = new List<ValidatorItem>
        {
            new(new StringLengthAttribute(10)),
            new(new CustomValidationAttribute(typeof(object), "Equals")),
            new(new RequiredAttribute())
        };

        provider.CreateValidators(new ModelValidatorProviderContext(metadata, results));

        results.Select(r => r.ValidatorMetadata.GetType()).ShouldBe([typeof(RequiredAttribute), typeof(StringLengthAttribute), typeof(CustomValidationAttribute)]);
        results[0].Validator.ShouldNotBeNull();
        results[0].IsReusable.ShouldBeTrue();
        results[2].Validator.ShouldBeNull();
    }

    [Fact]
    public void HasValidators_ValidationAttributes_ShouldReturnTrue()
    {
        var provider = new I18NextModelValidatorProvider(new I18NextValidationLocalizer(Substitute.For<II18Next>()));

        provider.HasValidators(typeof(string), [new RequiredAttribute()]).ShouldBeTrue();
        provider.HasValidators(typeof(string), [new object()]).ShouldBeFalse();
    }

    private static IDictionary<string, string> AddValidation(IModelMetadataProvider metadataProvider, IValidationAttributeAdapterProvider adapterProvider,
        string property, ValidationAttribute attribute)
    {
        var metadata = metadataProvider.GetMetadataForProperty(typeof(PersonModel), property);
        var attributes = new Dictionary<string, string>();
        var context = new ClientModelValidationContext(new ActionContext(), metadata, metadataProvider, attributes);

        adapterProvider.GetAttributeAdapter(attribute, null).AddValidation(context);

        return attributes;
    }

    public class DisplayModel
    {
        public string Name { get; set; }

        [Display(Name = "fields.email")]
        public string Email { get; set; }

        [DisplayName("fields.city")]
        public string City { get; set; }

        [Display(Order = 1)]
        [DisplayName("Name")]
        public string Order { get; set; }

        [Display(Name = nameof(I18NextValidationLocalizerFixture.Messages.Required), ResourceType = typeof(I18NextValidationLocalizerFixture.Messages))]
        public string Resource { get; set; }

        public string Missing { get; set; }
    }
}
