using System.Threading.Tasks;

using I18Next.Net.Backends;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class InMemoryBackendFixture
{
    public InMemoryBackendFixture()
    {
        _backend = new InMemoryBackend();

        _backend.AddTranslation("en-US", "test", "Value1", "Translated value 1");
        _backend.AddTranslation("en-US", "test", "Value2", "Translated value 2");
        _backend.AddTranslation("en-US", "test", "SectionA.Value1", "Translated section value 1");
        _backend.AddTranslation("en-US", "test", "SectionA.Value2", "Translated section value 2");

        _backend.AddTranslation("de", "test", "Value1", "Translated value 1");
        _backend.AddTranslation("de", "test", "Value2", "Translated value 2");
        _backend.AddTranslation("de", "test", "SectionA.Value1", "Translated section value 1");
        _backend.AddTranslation("de", "test", "SectionA.Value2", "Translated section value 2");
    }

    private readonly InMemoryBackend _backend;

    [Fact]
    public async Task AddTranslation_AlterExistingEntry_TranslationShouldBeAltered()
    {
        _backend.AddTranslation("de", "test", "Value2", "Altered translated value");

        var tree = await _backend.LoadNamespaceAsync("de", "test");

        tree.ShouldNotBeNull();

        var value = tree.GetValue("Value2", null);

        value.ShouldBe("Altered translated value");
    }

    [Fact]
    public async Task AddTranslation_AlterExistingNestedEntry_TranslationShouldBeAltered()
    {
        _backend.AddTranslation("de", "test", "SectionA.Value1", "Altered nested translated value");

        var tree = await _backend.LoadNamespaceAsync("de", "test");

        tree.ShouldNotBeNull();

        var value = tree.GetValue("SectionA.Value1", null);

        value.ShouldBe("Altered nested translated value");
    }

    [Fact]
    public async Task AddTranslation_NewNestedTranslation_TranslationShouldBeAdded()
    {
        _backend.AddTranslation("fr", "test", "SectionX.ValueX", "New nested translated value");

        var tree = await _backend.LoadNamespaceAsync("fr", "test");

        tree.ShouldNotBeNull();

        var value = tree.GetValue("SectionX.ValueX", null);

        value.ShouldBe("New nested translated value");
    }

    [Fact]
    public async Task AddTranslation_NewTranslation_TranslationShouldBeAdded()
    {
        _backend.AddTranslation("fr", "test", "ValueX", "New translated value");

        var tree = await _backend.LoadNamespaceAsync("fr", "test");

        tree.ShouldNotBeNull();

        var value = tree.GetValue("ValueX", null);

        value.ShouldBe("New translated value");
    }

    [Fact]
    public async Task LoadNamespaceAsync_ExtractLanguagePart_ShouldProvideTranslationsForOnlyTheLanguagePart()
    {
        var tree = await _backend.LoadNamespaceAsync("de-DE", "test");

        tree.ShouldNotBeNull();

        tree.GetValue("Value1", null).ShouldBe("Translated value 1");
        tree.GetValue("Value2", null).ShouldBe("Translated value 2");
    }

    [Fact]
    public async Task LoadNamespaceAsync_NestedKeys_ShouldProvideCorrectTranslations()
    {
        var tree = await _backend.LoadNamespaceAsync("en-US", "test");

        tree.ShouldNotBeNull();

        tree.GetValue("SectionA.Value1", null).ShouldBe("Translated section value 1");
        tree.GetValue("SectionA.Value2", null).ShouldBe("Translated section value 2");
    }

    [Fact]
    public async Task LoadNamespaceAsync_RootKeys_ShouldProvideCorrectTranslations()
    {
        var tree = await _backend.LoadNamespaceAsync("en-US", "test");

        tree.ShouldNotBeNull();

        tree.GetValue("Value1", null).ShouldBe("Translated value 1");
        tree.GetValue("Value2", null).ShouldBe("Translated value 2");
    }
}
