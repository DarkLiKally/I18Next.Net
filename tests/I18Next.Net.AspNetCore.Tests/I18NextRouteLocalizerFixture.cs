using System;
using System.Threading.Tasks;

using I18Next.Net.Backends;

using Shouldly;

using Xunit;

namespace I18Next.Net.AspNetCore.Tests;

public class I18NextRouteLocalizerFixture
{
    private readonly I18NextRouteLocalizer _localizer;

    public I18NextRouteLocalizerFixture()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("de", "routes", "products", "produkte");
        backend.AddTranslation("de", "routes", "search", "suche nach");
        backend.AddTranslation("de", "routes", "invalid", "a/b");
        backend.AddTranslation("en-US", "routes", "products", "goods");

        _localizer = new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions { Languages = ["en-US", "de", "DE"] });
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("DE", "de")]
    [InlineData("de-AT", "de")]
    [InlineData("en", "en-US")]
    [InlineData("en-GB", "en-US")]
    [InlineData("fr", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void MatchLanguage_ShouldFindConfiguredLanguage(string language, string expected)
    {
        _localizer.MatchLanguage(language).ShouldBe(expected);
    }

    [Fact]
    public void Constructor_DuplicateLanguages_ShouldBeIgnored()
    {
        _localizer.Languages.ShouldBe(["en-US", "de"]);
        _localizer.DefaultLanguage.ShouldBe("en-US");
        _localizer.Namespace.ShouldBe("routes");
    }

    [Theory]
    [InlineData("/products/42", "de", "/de/produkte/42")]
    [InlineData("/Products/42/", "de", "/de/produkte/42/")]
    [InlineData("/products/42", "en", "/en-US/goods/42")]
    [InlineData("/search?q=products#results", "de", "/de/suche%20nach?q=products#results")]
    [InlineData("/invalid/x%2Fy", "de", "/de/invalid/x%2Fy")]
    [InlineData("/", "de", "/de")]
    [InlineData("", "de", "/de")]
    public async Task LocalizePathAsync_ShouldTranslateSegments(string path, string language, string expected)
    {
        (await _localizer.LocalizePathAsync(path, language)).ShouldBe(expected);
        _localizer.LocalizePath(path, language).ShouldBe(expected);
    }

    [Fact]
    public async Task LocalizePathAsync_InvalidArguments_ShouldThrow()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _localizer.LocalizePathAsync(null, "de"));
        await Should.ThrowAsync<ArgumentException>(() => _localizer.LocalizePathAsync("products", "de"));
        await Should.ThrowAsync<ArgumentException>(() => _localizer.LocalizePathAsync("?q=1", "de"));
        await Should.ThrowAsync<ArgumentException>(() => _localizer.LocalizePathAsync("https://example.com/products", "de"));
        await Should.ThrowAsync<ArgumentException>(() => _localizer.LocalizePathAsync("/products", "fr"));
    }

    [Fact]
    public void Constructor_InvalidOptions_ShouldThrow()
    {
        var backend = new InMemoryBackend();

        Should.Throw<ArgumentNullException>(() => new I18NextRouteLocalizer(null, new I18NextLocalizedRoutesOptions { Languages = ["en"] }));
        Should.Throw<ArgumentNullException>(() => new I18NextRouteLocalizer(backend, null));
        Should.Throw<ArgumentException>(() => new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions()));
        Should.Throw<ArgumentException>(() => new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions { Languages = ["en/de"] }));
        Should.Throw<ArgumentException>(() => new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions { Languages = [""] }));
        Should.Throw<ArgumentException>(() => new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions { Languages = ["en"], DefaultLanguage = "de" }));
        Should.Throw<ArgumentException>(() => new I18NextRouteLocalizer(backend, new I18NextLocalizedRoutesOptions { Languages = ["en"], Namespace = "" }));
    }

    [Fact]
    public void Constructor_DefaultLanguage_ShouldBeMatched()
    {
        var localizer = new I18NextRouteLocalizer(new InMemoryBackend(), new I18NextLocalizedRoutesOptions { Languages = ["en", "de"], DefaultLanguage = "de-CH" });

        localizer.DefaultLanguage.ShouldBe("de");
    }
}
