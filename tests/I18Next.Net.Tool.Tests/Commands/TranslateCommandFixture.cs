using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class TranslateCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();
    private readonly FakeTranslationHandler _handler = new();

    public TranslateCommandFixture()
    {
        _context.HttpMessageHandler = _handler;
        _context.Write("locales/en/translation.json", """
                                                      {
                                                        "greeting": "Hello {{name}}",
                                                        "menu": { "title": "Menu" },
                                                        "item_one": "{{count}} item",
                                                        "item_other": "{{count}} items",
                                                        "empty": ""
                                                      }
                                                      """);
        _context.Write("locales/de/translation.json", """{ "greeting": "Hallo {{name}}", "menu": { "title": "" } }""");
        _context.Write("locales/ru/translation.json", "{}");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Translate_DeepL_ShouldFillMissingAndEmptyTranslations()
    {
        _context.EnvironmentVariables["DEEPL_AUTH_KEY"] = "secret:fx";

        (await Run()).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldBe("""
                                 locales/de/translation.json: translated 3 keys from en.
                                 locales/ru/translation.json: translated 6 keys from en.

                                 """, StringCompareShould.IgnoreLineEndings);
        _context.Read("locales/de/translation.json").ShouldBe("""
                                                             {
                                                               "greeting": "Hallo {{name}}",
                                                               "menu": {
                                                                 "title": "[DE] Menu"
                                                               },
                                                               "item_one": "[DE] {{count}} item",
                                                               "item_other": "[DE] {{count}} items"
                                                             }

                                                             """.Replace("\r\n", "\n"));
        _context.Read("locales/ru/translation.json").ShouldContain("\"item_few\": \"[RU] {{count}} items\"");

        var request = _handler.Requests[0];
        request.Uri.ShouldBe("https://api-free.deepl.com/v2/translate");
        request.Authorization.ShouldBe("DeepL-Auth-Key secret:fx");
        _handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Translate_Azure_ShouldUseKeyRegionAndEndpoint()
    {
        _context.EnvironmentVariables["AZURE_TRANSLATOR_REGION"] = "westeurope";

        (await Run("--provider", "azure", "--auth-key", "key", "--endpoint", "https://proxy.example.com/", "-l", "de")).ShouldBe(ExitCodes.Success);

        var request = _handler.Requests.ShouldHaveSingleItem();
        request.Uri.ShouldBe("https://proxy.example.com/translate?api-version=3.0&textType=html&to=de&from=en");
        request.SubscriptionKey.ShouldBe("key");
        request.Region.ShouldBe("westeurope");
        _context.Read("locales/de/translation.json").ShouldContain("\"title\": \"[de] Menu\"");
    }

    [Fact]
    public async Task Translate_DryRun_ShouldListTheKeysWithoutRequests()
    {
        (await Run("--dry-run", "--namespaces", "translation")).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("""
                                      locales/de/translation.json: would translate 3 keys.
                                        menu.title
                                        item_one
                                        item_other
                                      """, Case.Sensitive);
        _handler.Requests.ShouldBeEmpty();
        _context.Read("locales/ru/translation.json").ShouldBe("{}");
    }

    [Fact]
    public async Task Translate_NothingMissing_ShouldSucceedWithoutRequests()
    {
        (await Run("--languages", "en")).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldContain("No missing translations.");
        _handler.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("deepl", "DEEPL_AUTH_KEY")]
    [InlineData("azure", "AZURE_TRANSLATOR_KEY")]
    public async Task Translate_MissingKey_ShouldFail(string provider, string variable)
    {
        (await Run("--provider", provider)).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain(variable);
    }

    [Fact]
    public async Task Translate_InvalidEndpointOrSource_ShouldFail()
    {
        (await Run("--auth-key", "key", "--endpoint", "not a url")).ShouldBe(ExitCodes.Error);
        _context.Error.ShouldContain("is not an absolute URL");

        (await Run("--source-language", "fr")).ShouldBe(ExitCodes.Error);
        _context.Error.ShouldContain("does not exist");
    }

    [Fact]
    public async Task Translate_ServiceError_ShouldFail()
    {
        _handler.StatusCode = HttpStatusCode.Forbidden;

        (await Run("--auth-key", "key")).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("DeepL returned 403");
    }

    private Task<int> Run(params string[] args)
    {
        return _context.RunAsync(["translate", "-p", _context.Locales, .. args]);
    }

    private sealed class FakeTranslationHandler : HttpMessageHandler
    {
        public List<(string Uri, string Authorization, string SubscriptionKey, string Region)> Requests { get; } = [];

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!.AbsoluteUri, Header(request, "Authorization"), Header(request, "Ocp-Apim-Subscription-Key"),
                Header(request, "Ocp-Apim-Subscription-Region")));

            if (StatusCode != HttpStatusCode.OK)
                return new HttpResponseMessage(StatusCode) { Content = new StringContent("""{ "message": "Forbidden" }""") };

            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            string json;

            if (body.RootElement.ValueKind == JsonValueKind.Array)
            {
                var language = request.RequestUri.Query.Split('&').Single(p => p.StartsWith("to=")).Substring(3);
                json = JsonSerializer.Serialize(body.RootElement.EnumerateArray()
                    .Select(t => new { translations = new[] { new { text = $"[{language}] {t.GetProperty("Text").GetString()}" } } }));
            }
            else
            {
                var language = body.RootElement.GetProperty("target_lang").GetString();
                json = JsonSerializer.Serialize(new
                {
                    translations = body.RootElement.GetProperty("text").EnumerateArray().Select(t => new { text = $"[{language}] {t.GetString()}" })
                });
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        private static string Header(HttpRequestMessage request, string name)
        {
            return request.Headers.TryGetValues(name, out var values) ? values.Single() : null;
        }
    }
}
