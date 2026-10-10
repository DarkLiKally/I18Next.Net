using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.AspNetCore.Tests;

public class MissingKeysEndpointFixture
{
    private readonly IMissingKeyHandler _handler = Substitute.For<IMissingKeyHandler>();
    private readonly IMissingKeyHandler _otherHandler = Substitute.For<IMissingKeyHandler>();

    private Task<IHost> StartAsync(Action<IEndpointRouteBuilder> configureEndpoints)
    {
        return TestApplication.StartAsync(services => services.AddI18NextLocalization(i18n => i18n
                .AddBackend(new InMemoryBackend())
                .AddMissingKeyHandler(_handler)
                .AddMissingKeyHandler(_otherHandler)),
            app => app.UseRouting().UseEndpoints(configureEndpoints));
    }

    [Fact]
    public async Task MapI18NextMissingKeys_ValidRequest_ShouldPassKeysToHandlers()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys());

        var response = await host.GetTestClient().PostAsync("/locales/add/de/translation", Json("""{ "title": "Title", "menu.home": "Home", "title": "" }"""));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        foreach (var handler in new[] { _handler, _otherHandler })
        {
            await handler.Received(2).HandleMissingKeyAsync(Arg.Any<object>(), Arg.Any<MissingKeyEventArgs>());
            await handler.Received(1).HandleMissingKeyAsync(Arg.Any<HttpContext>(),
                Arg.Is<MissingKeyEventArgs>(a => a.Language == "de" && a.Namespace == "translation" && a.Key == "title" && a.PossibleKeys.Length == 1 && a.DefaultValue == "Title"));
            await handler.Received(1).HandleMissingKeyAsync(Arg.Any<HttpContext>(), Arg.Is<MissingKeyEventArgs>(a => a.Key == "menu.home" && a.DefaultValue == "Home"));
        }
    }

    [Fact]
    public async Task MapI18NextMissingKeys_JsonWithCharset_ShouldBeAccepted()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys());
        var content = new StringContent("""{ "key": "value" }""", Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "UTF-8" };

        var response = await host.GetTestClient().PostAsync("/locales/add/en/common", content);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _handler.Received(1).HandleMissingKeyAsync(Arg.Any<object>(), Arg.Is<MissingKeyEventArgs>(a => a.Key == "key" && a.Namespace == "common"));
    }

    [Theory]
    [InlineData("/locales/add/fr/translation")]
    [InlineData("/locales/add/de/other")]
    [InlineData("/locales/add/..%2Fde/translation")]
    [InlineData("/locales/add/de/..%2Ftranslation")]
    public async Task MapI18NextMissingKeys_NotAllowedNames_ShouldReturnNotFound(string path)
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys(configure: o =>
        {
            o.Languages = ["de", "en"];
            o.Namespaces = ["translation"];
        }));

        var response = await host.GetTestClient().PostAsync(path, Json("""{ "key": "value" }"""));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await _handler.DidNotReceiveWithAnyArgs().HandleMissingKeyAsync(null, null);
    }

    [Theory]
    [InlineData("""[ "key" ]""")]
    [InlineData("""{ "key": """)]
    [InlineData("""{ "": "value" }""")]
    [InlineData("\"key\"")]
    [InlineData("")]
    public async Task MapI18NextMissingKeys_InvalidBody_ShouldReturnBadRequest(string body)
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys());

        var response = await host.GetTestClient().PostAsync("/locales/add/de/translation", Json(body));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _handler.DidNotReceiveWithAnyArgs().HandleMissingKeyAsync(null, null);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_TooManyKeys_ShouldReturnBadRequest()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys(configure: o => o.MaxKeys = 2));
        var client = host.GetTestClient();

        (await client.PostAsync("/locales/add/de/translation", Json("""{ "a": "", "b": "", "c": "" }"""))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _handler.DidNotReceiveWithAnyArgs().HandleMissingKeyAsync(null, null);

        (await client.PostAsync("/locales/add/de/translation", Json("""{ "a": "", "b": "" }"""))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_TooLargeBody_ShouldReturnPayloadTooLarge()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys(configure: o => o.MaxRequestBodySize = 20));
        var client = host.GetTestClient();
        var body = """{ "a-rather-long-key": "value" }""";

        (await client.PostAsync("/locales/add/de/translation", Json(body))).StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await client.PostAsync("/locales/add/de/translation", new UnknownLengthContent(body))).StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        await _handler.DidNotReceiveWithAnyArgs().HandleMissingKeyAsync(null, null);

        (await client.PostAsync("/locales/add/de/translation", new UnknownLengthContent("""{ "a": "" }"""))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_FormContent_ShouldReturnUnsupportedMediaType()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys());

        var response = await host.GetTestClient().PostAsync("/locales/add/de/translation", new StringContent("key=value", Encoding.UTF8, "application/x-www-form-urlencoded"));

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_GetRequest_ShouldNotMatch()
    {
        using var host = await StartAsync(e => e.MapI18NextMissingKeys());

        var response = await host.GetTestClient().GetAsync("/locales/add/de/translation");

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_RequireAuthorization_ShouldChallengeAnonymousRequests()
    {
        using var host = await TestApplication.StartAsync(services =>
            {
                services.AddI18NextLocalization(i18n => i18n.AddBackend(new InMemoryBackend()).AddMissingKeyHandler(_handler));
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
                services.AddAuthorization();
            },
            app => app.UseRouting().UseAuthentication().UseAuthorization().UseEndpoints(e => e.MapI18NextMissingKeys().RequireAuthorization()));

        var response = await host.GetTestClient().PostAsync("/locales/add/de/translation", Json("""{ "key": "value" }"""));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.ShouldBe("/Account/Login");
        await _handler.DidNotReceiveWithAnyArgs().HandleMissingKeyAsync(null, null);
    }

    [Fact]
    public async Task MapI18NextMissingKeys_InvalidConfiguration_ShouldThrow()
    {
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(e => e.MapI18NextMissingKeys("/missing/{lng}")));
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(e => e.MapI18NextMissingKeys(configure: o => o.MaxKeys = 0)));
        await Should.ThrowAsync<ArgumentException>(() => StartAsync(e => e.MapI18NextMissingKeys(configure: o => o.MaxRequestBodySize = 0)));
    }

    private static StringContent Json(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class UnknownLengthContent : HttpContent
    {
        private readonly byte[] _content;

        public UnknownLengthContent(string content)
        {
            _content = Encoding.UTF8.GetBytes(content);
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(_content, 0, _content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }
    }
}
