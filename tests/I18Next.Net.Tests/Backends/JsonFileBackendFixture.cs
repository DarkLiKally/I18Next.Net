using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class JsonFileBackendFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        _backend = new JsonFileBackend("TestFiles");
        _tree = await _backend.LoadNamespaceAsync("en-US", "test");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private JsonFileBackend _backend;
    private ITranslationTree _tree;

    [Fact]
    public async Task LoadNamespaceAsync_ExtractLanguagePart_ShouldProvideTranslationsForOnlyTheLanguagePart()
    {
        var tree = await _backend.LoadNamespaceAsync("de-DE", "test");

        tree.ShouldNotBeNull();

        _tree.GetValue("Value1", null).ShouldBe("Translated value 1");
        _tree.GetValue("Value2", null).ShouldBe("Translated value 2");
        _tree.GetValue("Value3", null).ShouldBe("  Translated value 3   ");
        _tree.GetValue("Value4", null).ShouldBe("Translated value 4");
        _tree.GetValue("Value5", null).ShouldBe("Translated value 5");
        _tree.GetValue("Value6", null).ShouldBe("Translated value 6");
    }

    [Fact]
    public void LoadNamespaceAsync_ShouldSetNamespace()
    {
        _tree.Namespace.ShouldBe("test");
    }

    [Fact]
    public void LoadNamespaceAsync_NestedKeys_ShouldProvideCorrectTranslations()
    {
        _tree.ShouldNotBeNull();

        _tree.GetValue("SectionA.Value1", null).ShouldBe("Translated value 1");
        _tree.GetValue("SectionA.Value2", null).ShouldBe("Translated value 2");
        _tree.GetValue("SectionA.Value3", null).ShouldBe("  Translated value 3   ");
        _tree.GetValue("SectionA.Value4", null).ShouldBe("Translated value 4");
        _tree.GetValue("SectionA.Value5", null).ShouldBe("Translated value 5");
        _tree.GetValue("SectionA.Value6", null).ShouldBe("Translated value 6");

        _tree.GetValue("SectionB.SubSectionA.Value1", null).ShouldBe("Translated value 1");
        _tree.GetValue("SectionB.SubSectionA.Value2", null).ShouldBe("Translated value 2");
        _tree.GetValue("SectionB.SubSectionA.Value3", null).ShouldBe("  Translated value 3   ");
        _tree.GetValue("SectionB.SubSectionA.Value4", null).ShouldBe("Translated value 4");
        _tree.GetValue("SectionB.SubSectionA.Value5", null).ShouldBe("Translated value 5");
        _tree.GetValue("SectionB.SubSectionA.Value6", null).ShouldBe("Translated value 6");
    }

    [Fact]
    public void LoadNamespaceAsync_RootKeys_ShouldProvideCorrectTranslations()
    {
        _tree.ShouldNotBeNull();

        _tree.GetValue("Value1", null).ShouldBe("Translated value 1");
        _tree.GetValue("Value2", null).ShouldBe("Translated value 2");
        _tree.GetValue("Value3", null).ShouldBe("  Translated value 3   ");
        _tree.GetValue("Value4", null).ShouldBe("Translated value 4");
        _tree.GetValue("Value5", null).ShouldBe("Translated value 5");
        _tree.GetValue("Value6", null).ShouldBe("Translated value 6");
    }
}
