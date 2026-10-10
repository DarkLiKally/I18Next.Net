using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class DeepLTranslatorFixture
{
    private readonly FakeHttpMessageHandler _handler = new(EchoUppercase);

    [Fact]
    public async Task TranslateAsync_FreeKey_ShouldPostToTheFreeEndpoint()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret:fx");

        var result = await translator.TranslateAsync(["Hello", "World"], "en-US", "de-DE");

        result.ShouldBe(["HELLO", "WORLD"]);

        var request = _handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ShouldBe(new Uri("https://api-free.deepl.com/v2/translate"));
        request.Headers["Authorization"].ShouldBe("DeepL-Auth-Key secret:fx");
        request.ContentType.ShouldBe("application/json; charset=utf-8");

        var json = request.Json;
        json.GetProperty("text").EnumerateArray().Select(t => t.GetString()).ShouldBe(["Hello", "World"]);
        json.GetProperty("source_lang").GetString().ShouldBe("EN");
        json.GetProperty("target_lang").GetString().ShouldBe("DE");
        json.GetProperty("tag_handling").GetString().ShouldBe("xml");
        json.GetProperty("ignore_tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["x"]);
        json.TryGetProperty("formality", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task TranslateAsync_ProKey_ShouldPostToTheProEndpoint()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret");

        await translator.TranslateAsync("Hello", "en", "fr");

        _handler.Requests[0].Uri.ShouldBe(new Uri("https://api.deepl.com/v2/translate"));
    }

    [Fact]
    public async Task TranslateAsync_Options_ShouldBeSent()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret")
        {
            Endpoint = new Uri("https://proxy.example.com/deepl/"),
            Formality = "less",
            LanguageMappings = { ["no"] = "NB" }
        };

        await translator.TranslateAsync("Hello", null, "no");
        await translator.TranslateAsync("Hello", "en", "pt-br");
        await translator.TranslateAsync("Hello", "en", "zh-Hans");

        _handler.Requests[0].Uri.ShouldBe(new Uri("https://proxy.example.com/deepl/v2/translate"));
        _handler.Requests[0].Json.TryGetProperty("source_lang", out _).ShouldBeFalse();
        _handler.Requests[0].Json.GetProperty("target_lang").GetString().ShouldBe("NB");
        _handler.Requests[0].Json.GetProperty("formality").GetString().ShouldBe("less");
        _handler.Requests[1].Json.GetProperty("target_lang").GetString().ShouldBe("PT-BR");
        _handler.Requests[2].Json.GetProperty("target_lang").GetString().ShouldBe("ZH-HANS");
    }

    [Fact]
    public async Task TranslateAsync_Placeholders_ShouldSurviveTheTranslation()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret");

        var result = await translator.TranslateAsync("Hello <b>{{name}}</b> & $t(common:more) {{count, number}}", "en", "de");

        result.ShouldBe("HELLO <b>{{name}}</b> & $t(common:more) {{count, number}}");
        _handler.Requests[0].Json.GetProperty("text")[0].GetString().ShouldBe(
            "Hello <x data-i=\"0\">&lt;b&gt;</x><x data-i=\"1\">{{name}}</x><x data-i=\"2\">&lt;/b&gt;</x> &amp; <x data-i=\"3\">$t(common:more)</x> "
            + "<x data-i=\"4\">{{count, number}}</x>");
    }

    [Fact]
    public async Task TranslateAsync_ManyTexts_ShouldBeSentInBatches()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret");
        var texts = Enumerable.Range(0, 120).Select(i => $"text {i}").ToList();

        var result = await translator.TranslateAsync(texts, "en", "de");

        result.ShouldBe(texts.Select(t => t.ToUpperInvariant()));
        _handler.Requests.Select(r => r.Json.GetProperty("text").GetArrayLength()).ShouldBe([50, 50, 20]);
    }

    [Fact]
    public async Task TranslateAsync_NoTexts_ShouldNotSendARequest()
    {
        var translator = new DeepLTranslator(new HttpClient(_handler), "secret");

        (await translator.TranslateAsync([], "en", "de")).ShouldBeEmpty();
        _handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task TranslateAsync_ErrorResponse_ShouldThrowWithTheMessage()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{ "message": "Quota exceeded" }""", (HttpStatusCode)456));
        var translator = new DeepLTranslator(new HttpClient(handler), "secret");

        var exception = await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync("Hello", "en", "de"));

        exception.StatusCode.ShouldBe((HttpStatusCode)456);
        exception.Message.ShouldContain("DeepL returned 456");
        exception.Message.ShouldContain("Quota exceeded");
    }

    [Fact]
    public async Task TranslateAsync_PlainTextError_ShouldThrowWithTheBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("Wrong key") });
        var translator = new DeepLTranslator(new HttpClient(handler), "secret");

        var exception = await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync("Hello", "en", "de"));

        exception.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        exception.Message.ShouldEndWith("Wrong key");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{ "translations": {} }""")]
    [InlineData("""{ "translations": [] }""")]
    [InlineData("""{ "translations": [ { "text": 1 } ] }""")]
    [InlineData("""{ "translations": [ "x" ] }""")]
    public async Task TranslateAsync_UnexpectedResponse_ShouldThrow(string body)
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(body));
        var translator = new DeepLTranslator(new HttpClient(handler), "secret");

        await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync("Hello", "en", "de"));
    }

    [Fact]
    public async Task Constructor_InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new DeepLTranslator(" "));
        Should.Throw<ArgumentNullException>(() => new DeepLTranslator(null, "key"));
        new DeepLTranslator("key:fx").Endpoint.ShouldBe(new Uri(DeepLTranslator.FreeEndpoint));

        var translator = new DeepLTranslator(new HttpClient(_handler), "secret");
        await Should.ThrowAsync<ArgumentNullException>(() => translator.TranslateAsync(null, "en", "de"));
        await Should.ThrowAsync<ArgumentException>(() => translator.TranslateAsync(["a"], "en", ""));
    }

    private static HttpResponseMessage EchoUppercase(CapturedRequest request)
    {
        var texts = request.Json.GetProperty("text").EnumerateArray().Select(t => new { text = Uppercase(t.GetString()) });

        return FakeHttpMessageHandler.Json(JsonSerializer.Serialize(new { translations = texts }));
    }

    private static string Uppercase(string text)
    {
        var start = text.IndexOf('<');

        return start < 0 ? text.ToUpperInvariant() : text.Substring(0, start).ToUpperInvariant() + text.Substring(start);
    }
}
