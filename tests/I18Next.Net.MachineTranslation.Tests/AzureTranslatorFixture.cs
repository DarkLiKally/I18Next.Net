using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class AzureTranslatorFixture
{
    private readonly FakeHttpMessageHandler _handler = new(Reverse);

    [Fact]
    public async Task TranslateAsync_ShouldPostHtmlTextsWithKeyAndRegion()
    {
        var translator = new AzureTranslator(new HttpClient(_handler), "secret", "westeurope");

        var result = await translator.TranslateAsync(["abc", "Hi {{name}}"], "en-US", "de-DE");

        result.ShouldBe(["cba", "{{name}} iH"]);

        var request = _handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ShouldBe(new Uri("https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&textType=html&to=de&from=en"));
        request.Headers["Ocp-Apim-Subscription-Key"].ShouldBe("secret");
        request.Headers["Ocp-Apim-Subscription-Region"].ShouldBe("westeurope");
        request.ContentType.ShouldBe("application/json; charset=utf-8");
        request.Json.EnumerateArray().Select(t => t.GetProperty("Text").GetString()).ShouldBe([
            "abc",
            "Hi <span class=\"notranslate\" data-i=\"0\">{{name}}</span>"
        ]);
    }

    [Fact]
    public async Task TranslateAsync_NoRegionOrSource_ShouldBeOmitted()
    {
        var translator = new AzureTranslator(new HttpClient(_handler), "secret");

        await translator.TranslateAsync("abc", null, "zh-Hans");

        var request = _handler.Requests.ShouldHaveSingleItem();
        request.Uri.Query.ShouldBe("?api-version=3.0&textType=html&to=zh-Hans");
        request.Headers.ContainsKey("Ocp-Apim-Subscription-Region").ShouldBeFalse();
    }

    [Fact]
    public async Task TranslateAsync_CustomEndpointAndMappings_ShouldBeUsed()
    {
        var translator = new AzureTranslator(new HttpClient(_handler), "secret")
        {
            Endpoint = new Uri("https://my.cognitiveservices.azure.com/translator/text/v3.0"),
            LanguageMappings = { ["zh-CN"] = "zh-Hans" }
        };

        await translator.TranslateAsync("abc", "fr-CA", "zh-CN");

        _handler.Requests[0].Uri.ShouldBe(
            new Uri("https://my.cognitiveservices.azure.com/translator/text/v3.0/translate?api-version=3.0&textType=html&to=zh-Hans&from=fr-CA"));
    }

    [Fact]
    public async Task TranslateAsync_ManyTexts_ShouldBeSentInBatches()
    {
        var translator = new AzureTranslator(new HttpClient(_handler), "secret");
        var texts = Enumerable.Range(0, 150).Select(i => $"text {i}").ToList();
        texts.Add(new string('a', 39990));

        var result = await translator.TranslateAsync(texts, "en", "de");

        result.Count.ShouldBe(151);
        result[0].ShouldBe("0 txet");
        _handler.Requests.Select(r => r.Json.GetArrayLength()).ShouldBe([100, 50, 1]);
    }

    [Fact]
    public async Task TranslateAsync_ErrorResponse_ShouldThrowWithTheMessage()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json("""{ "error": { "code": 401000, "message": "Invalid key" } }""", HttpStatusCode.Unauthorized));
        var translator = new AzureTranslator(new HttpClient(handler), "secret");

        var exception = await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync("Hello", "en", "de"));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        exception.Message.ShouldBe("Azure AI Translator returned 401 (Unauthorized): Invalid key");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""[ 1 ]""")]
    [InlineData("""[ { "translations": [] } ]""")]
    [InlineData("""[ { "translations": [ 1 ] } ]""")]
    [InlineData("""[ { "translations": [ { "to": "de" } ] } ]""")]
    [InlineData("""[ { "translations": [ { "text": null } ] } ]""")]
    public async Task TranslateAsync_UnexpectedResponse_ShouldThrow(string body)
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(body));
        var translator = new AzureTranslator(new HttpClient(handler), "secret");

        await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync("Hello", "en", "de"));
    }

    [Fact]
    public async Task Constructor_InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new AzureTranslator(""));
        Should.Throw<ArgumentNullException>(() => new AzureTranslator((HttpClient)null, "key"));
        new AzureTranslator("key", "region").Endpoint.ShouldBe(new Uri(AzureTranslator.DefaultEndpoint));

        var translator = new AzureTranslator(new HttpClient(_handler), "secret");
        await Should.ThrowAsync<ArgumentNullException>(() => translator.TranslateAsync(null, "en", "de"));
        await Should.ThrowAsync<ArgumentException>(() => translator.TranslateAsync(["a"], "en", null));
    }

    private static HttpResponseMessage Reverse(CapturedRequest request)
    {
        var items = request.Json.EnumerateArray()
            .Select(t => new { translations = new[] { new { text = ReverseText(t.GetProperty("Text").GetString()), to = "de" } } });

        return FakeHttpMessageHandler.Json(JsonSerializer.Serialize(items));
    }

    private static string ReverseText(string text)
    {
        var start = text.IndexOf('<');

        if (start < 0)
            return new string(text.Reverse().ToArray());

        return text.Substring(start) + " " + new string(text.Substring(0, start).Trim().Reverse().ToArray());
    }
}
