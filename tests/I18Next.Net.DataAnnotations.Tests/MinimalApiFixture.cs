#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

using Shouldly;

using Xunit;

namespace I18Next.Net.DataAnnotations.Tests;

public class MinimalApiFixture
{
    private static IServiceCollection AddValidation(IServiceCollection services)
    {
        return services.AddValidation();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        await using var app = await TestApplication.StartAsync(builder => AddValidation(builder.Services),
            app =>
            {
                app.MapPost("/people", (PersonModel person) => Results.Ok(person.Name));
                app.MapGet("/count", ([Range(1, 10)] int count) => Results.Ok(count));
            },
            i18n => i18n.AddDataAnnotationsLocalization());

        return await app.GetTestClient().SendAsync(request);
    }

    private static async Task<Dictionary<string, string[]>> GetErrorsAsync(HttpRequestMessage request)
    {
        var response = await SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("errors").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    [Fact]
    public async Task Post_InvalidModelInGerman_ShouldReturnTranslatedMessages()
    {
        const string json = """{ "name": "Jo", "email": "invalid", "confirmEmail": "other", "age": 5, "code": "abc" }""";

        var errors = await GetErrorsAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", json));

        errors["Name"].ShouldBe(["Das Feld Vorname muss eine Zeichenfolge mit einer minimalen Länge von 3 und einer maximalen Länge von 20 sein."]);
        errors["Email"].ShouldBe(["Das Feld E-Mail-Adresse enthält keine gültige E-Mail-Adresse."]);
        errors["ConfirmEmail"].ShouldBe(["'E-Mail-Bestätigung' und 'E-Mail-Adresse' stimmen nicht überein."]);
        errors["Age"].ShouldBe(["Das Feld Age muss zwischen 18 und 120 liegen."]);
        errors["Code"].ShouldBe(["Code darf nur Ziffern enthalten."]);
    }

    [Fact]
    public async Task Post_InvalidModelInEnglish_ShouldReturnDefaultMessagesWithTranslatedDisplayNames()
    {
        const string json = """{ "name": "Jo", "email": "invalid" }""";

        var errors = await GetErrorsAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "en", json));

        errors["Name"].ShouldBe(["The field Name must be a string with a minimum length of 3 and a maximum length of 20."]);
        errors["Email"].ShouldBe(["The E-mail/address field is not a valid e-mail address."]);
    }

    [Fact]
    public async Task Post_InvalidNestedRecord_ShouldReturnTranslatedMessages()
    {
        const string json = """{ "name": "Jane", "email": "jane@example.com", "confirmEmail": "jane@example.com", "address": { } }""";

        var errors = await GetErrorsAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", json));

        errors["Address.Street"].ShouldBe(["Das Feld Straße ist erforderlich."]);
        errors["Address.City"].ShouldBe(["Das Feld Ort ist erforderlich."]);
    }

    [Fact]
    public async Task Post_ValidModel_ShouldSucceed()
    {
        const string json = """{ "name": "Jane", "email": "jane@example.com", "confirmEmail": "jane@example.com" }""";

        var response = await SendAsync(TestApplication.CreateRequest(HttpMethod.Post, "/people", "de", json));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_InvalidParameter_ShouldReturnTranslatedMessage()
    {
        var errors = await GetErrorsAsync(TestApplication.CreateRequest(HttpMethod.Get, "/count?count=20", "de"));

        errors["count"].ShouldBe(["Das Feld Anzahl muss zwischen 1 und 10 liegen."]);
    }

    private static void Unvalidated(int value)
    {
    }

    [Fact]
    public void AddDataAnnotationsLocalization_ValidationOptions_ShouldInsertResolverOnce()
    {
        var services = new ServiceCollection();
        AddValidation(services);
        services.AddI18NextLocalization(i18n => i18n.AddDataAnnotationsLocalization().AddDataAnnotationsLocalization());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        options.Resolvers.Count(r => r.GetType().Name == "I18NextValidatableInfoResolver").ShouldBe(1);
        options.Resolvers[0].TryGetValidatableTypeInfo(typeof(PersonModel), out var typeInfo).ShouldBeFalse();
        typeInfo.ShouldBeNull();
        options.Resolvers[0].TryGetValidatableParameterInfo(typeof(MinimalApiFixture).GetMethod(nameof(Unvalidated), BindingFlags.NonPublic | BindingFlags.Static).GetParameters()[0], out var parameterInfo).ShouldBeFalse();
        parameterInfo.ShouldBeNull();
    }
}
#endif
