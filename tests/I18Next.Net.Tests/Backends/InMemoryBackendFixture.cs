using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

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

    [Fact]
    public async Task SaveNamespaceAsync_ExistingNamespace_ShouldReplaceTranslations()
    {
        var source = new DictionaryTranslationTree("test") { ["Value3"] = "Saved value 3", ["SectionB.Value1"] = "Saved section value 1" };

        await _backend.SaveNamespaceAsync("de", "test", source);

        var tree = await _backend.LoadNamespaceAsync("de", "test");

        tree.Namespace.ShouldBe("test");
        tree.GetAllValues().ShouldBe(new Dictionary<string, string> { ["Value3"] = "Saved value 3", ["SectionB.Value1"] = "Saved section value 1" },
            true);
    }

    [Fact]
    public async Task SaveNamespaceAsync_HierarchicalTree_ShouldStoreCopyOfAllValues()
    {
        var builder = new HierarchicalTranslationTreeBuilder { Namespace = "other" };
        builder.AddTranslation("menu.home", "Start");
        builder.AddTranslation("menu.about", "Über uns");
        var source = builder.Build();

        await _backend.SaveNamespaceAsync("de-AT", "other", source);
        source.Namespace = "changed";

        _backend.HasNamespace("de-AT", "other").ShouldBeTrue();

        var tree = await _backend.LoadNamespaceAsync("de-AT", "other");

        tree.Namespace.ShouldBe("other");
        tree.GetValue("menu.about", null).ShouldBe("Über uns");
        ((IHierarchicalTranslationTree)tree).GetGroupValues("menu").Count.ShouldBe(2);
        (await _backend.LoadNamespaceAsync("de", "other")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveNamespaceAsync_InvalidArguments_ShouldThrow()
    {
        var tree = new DictionaryTranslationTree("test");

        await Should.ThrowAsync<ArgumentException>(() => _backend.SaveNamespaceAsync(" ", "test", tree));
        await Should.ThrowAsync<ArgumentException>(() => _backend.SaveNamespaceAsync("de", null, tree));
        await Should.ThrowAsync<ArgumentNullException>(() => _backend.SaveNamespaceAsync("de", "test", null));
    }

    [Fact]
    public async Task RemoveNamespace_ExistingNamespace_ShouldFallBackToLanguagePart()
    {
        _backend.AddTranslation("de-AT", "test", "Value1", "Servus");

        (await _backend.LoadNamespaceAsync("de-AT", "test")).GetValue("Value1", null).ShouldBe("Servus");

        _backend.RemoveNamespace("de-AT", "test").ShouldBeTrue();
        _backend.RemoveNamespace("de-AT", "test").ShouldBeFalse();
        _backend.HasNamespace("de-AT", "test").ShouldBeFalse();

        (await _backend.LoadNamespaceAsync("de-AT", "test")).GetValue("Value1", null).ShouldBe("Translated value 1");
    }
}
