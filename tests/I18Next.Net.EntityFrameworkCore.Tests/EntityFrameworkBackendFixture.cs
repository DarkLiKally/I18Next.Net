using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Xunit;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public class EntityFrameworkBackendFixture : IDisposable
{
    private readonly EntityFrameworkBackend<TestDbContext> _backend;
    private readonly List<TranslationsChangedEventArgs> _changes = [];
    private readonly TestDatabase _database = new();

    public EntityFrameworkBackendFixture()
    {
        _backend = new EntityFrameworkBackend<TestDbContext>(_database.ContextFactory);
        _backend.TranslationsChanged += (_, e) => _changes.Add(e);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task LoadNamespaceAsync_ExistingNamespace_ShouldBuildHierarchicalTree()
    {
        await _database.AddAsync("en", "translation", "greeting", "Hello {{name}}");
        await _database.AddAsync("en", "translation", "menu.home", "Home");
        await _database.AddAsync("en", "translation", "menu.about", "About");
        await _database.AddAsync("en", "common", "greeting", "Hi");
        await _database.AddAsync("de", "translation", "greeting", "Hallo");

        var tree = await _backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<TranslationTree>();
        tree.Namespace.ShouldBe("translation");
        tree.GetAllValues().ShouldBe(new Dictionary<string, string>
        {
            ["greeting"] = "Hello {{name}}",
            ["menu.home"] = "Home",
            ["menu.about"] = "About"
        }, true);
        ((IHierarchicalTranslationTree)tree).GetGroupValues("menu").Keys.ShouldBe(["home", "about"]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_UnknownNamespace_ShouldReturnNull()
    {
        await _database.AddAsync("en", "translation", "greeting", "Hello");

        (await _backend.LoadNamespaceAsync("en", "common")).ShouldBeNull();
        (await _backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
        (await _backend.LoadNamespaceAsync("fr-CA", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageWithoutTranslations_ShouldFallBackToLanguagePart()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de-AT", "translation", "untranslated", null);

        var tree = await _backend.LoadNamespaceAsync("de-AT", "translation");

        tree.GetAllValues().ShouldBe(new Dictionary<string, string> { ["greeting"] = "Hallo" });
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageWithTranslations_ShouldNotFallBack()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de-AT", "translation", "greeting", "Servus");

        (await _backend.LoadNamespaceAsync("de-AT", "translation")).GetAllValues().ShouldBe(new Dictionary<string, string> { ["greeting"] = "Servus" });
    }

    [Fact]
    public async Task LoadNamespaceAsync_FallbackDisabled_ShouldReturnNull()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        _backend.FallbackToLanguagePart = false;

        (await _backend.LoadNamespaceAsync("de-AT", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_EntriesWithoutValue_ShouldBeIgnored()
    {
        await _database.AddAsync("en", "translation", "untranslated", null);

        (await _backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();

        await _database.AddAsync("en", "translation", "greeting", "Hello");

        (await _backend.LoadNamespaceAsync("en", "translation")).GetAllValues().Keys.ShouldBe(["greeting"]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_FlatTreeBuilder_ShouldKeepKeys()
    {
        var backend = new EntityFrameworkBackend<TestDbContext>(_database.ContextFactory,
            new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
        await _database.AddAsync("en", "translation", "Hello. How are you?", "Hi");
        await _database.AddAsync("en", "translation", "Hello", "Hello");

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.Namespace.ShouldBe("translation");
        tree.GetValue("Hello. How are you?", null).ShouldBe("Hi");
    }

    [Fact]
    public async Task SetValueAsync_NewKey_ShouldAddTranslationAndNotify()
    {
        await _backend.SetValueAsync("de", "translation", "greeting", "Hallo");

        var entry = _database.GetEntries().ShouldHaveSingleItem();
        entry.Language.ShouldBe("de");
        entry.Namespace.ShouldBe("translation");
        entry.Key.ShouldBe("greeting");
        entry.Value.ShouldBe("Hallo");

        var change = _changes.ShouldHaveSingleItem();
        change.Language.ShouldBe("de");
        change.Namespace.ShouldBe("translation");
    }

    [Fact]
    public async Task SetValueAsync_ExistingKey_ShouldUpdateTranslation()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de", "translation", "untranslated", null);

        await _backend.SetValueAsync("de", "translation", "greeting", "Servus");
        await _backend.SetValueAsync("de", "translation", "untranslated", "Übersetzt");

        _database.GetEntries().Select(e => (e.Key, e.Value)).ShouldBe([("greeting", "Servus"), ("untranslated", "Übersetzt")]);
        _changes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RemoveValueAsync_ExistingKey_ShouldRemoveTranslationAndNotify()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de", "translation", "farewell", "Tschüss");

        (await _backend.RemoveValueAsync("de", "translation", "greeting")).ShouldBeTrue();

        _database.GetEntries().ShouldHaveSingleItem().Key.ShouldBe("farewell");
        _changes.ShouldHaveSingleItem().Language.ShouldBe("de");
    }

    [Fact]
    public async Task RemoveValueAsync_UnknownKey_ShouldReturnFalse()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");

        (await _backend.RemoveValueAsync("de", "translation", "farewell")).ShouldBeFalse();
        (await _backend.RemoveValueAsync("en", "translation", "greeting")).ShouldBeFalse();

        _database.GetEntries().Count.ShouldBe(1);
        _changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveNamespaceAsync_ShouldReplaceTranslationsAndKeepMissingKeys()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de", "translation", "obsolete", "Alt");
        await _database.AddAsync("de", "translation", "untranslated", null);
        await _database.AddAsync("de", "translation", "filled", null);
        await _database.AddAsync("de", "common", "obsolete", "Bleibt");

        var tree = new DictionaryTranslationTree("translation")
        {
            ["greeting"] = "Servus",
            ["filled"] = "Gefüllt",
            ["menu.home"] = "Start"
        };

        await _backend.SaveNamespaceAsync("de", "translation", tree);

        _database.GetEntries().Select(e => (e.Language, e.Namespace, e.Key, e.Value)).OrderBy(e => e.Namespace).ThenBy(e => e.Key).ShouldBe([
            ("de", "common", "obsolete", "Bleibt"),
            ("de", "translation", "filled", "Gefüllt"),
            ("de", "translation", "greeting", "Servus"),
            ("de", "translation", "menu.home", "Start"),
            ("de", "translation", "untranslated", null)
        ]);
        _changes.ShouldHaveSingleItem().Namespace.ShouldBe("translation");
    }

    [Fact]
    public async Task SaveNamespaceAsync_HierarchicalTree_ShouldLoadSameValues()
    {
        var builder = new HierarchicalTranslationTreeBuilder { Namespace = "translation" };
        builder.AddTranslation("menu.home", "Start");
        builder.AddTranslation("menu.items.0", "Erstes");
        var source = builder.Build();

        await _backend.SaveNamespaceAsync("de-AT", "translation", source);

        (await _backend.LoadNamespaceAsync("de-AT", "translation")).GetAllValues().ShouldBe(source.GetAllValues(), true);
    }

    [Fact]
    public async Task InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new EntityFrameworkBackend<TestDbContext>(null));
        Should.Throw<ArgumentNullException>(() => new EntityFrameworkBackend<TestDbContext>(_database.ContextFactory, null));

        var tree = new DictionaryTranslationTree("translation");

        await Should.ThrowAsync<ArgumentException>(() => _backend.SaveNamespaceAsync("", "translation", tree));
        await Should.ThrowAsync<ArgumentException>(() => _backend.SaveNamespaceAsync("de", " ", tree));
        await Should.ThrowAsync<ArgumentNullException>(() => _backend.SaveNamespaceAsync("de", "translation", null));
        await Should.ThrowAsync<ArgumentException>(() => _backend.SetValueAsync(null, "translation", "key", "value"));
        await Should.ThrowAsync<ArgumentException>(() => _backend.SetValueAsync("de", "translation", "", "value"));
        await Should.ThrowAsync<ArgumentNullException>(() => _backend.SetValueAsync("de", "translation", "key", null));
        await Should.ThrowAsync<ArgumentException>(() => _backend.RemoveValueAsync("de", null, "key"));
        await Should.ThrowAsync<ArgumentException>(() => _backend.RemoveValueAsync("de", "translation", " "));

        _database.GetEntries().ShouldBeEmpty();
        _changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task UniqueIndex_DuplicateKey_ShouldThrow()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        await _database.AddAsync("de-AT", "translation", "greeting", "Servus");

        await Should.ThrowAsync<DbUpdateException>(() => _database.AddAsync("de", "translation", "greeting", "Grüß Gott"));
    }

    [Fact]
    public async Task Translator_ChangedValue_ShouldBeTranslatedAgain()
    {
        await _database.AddAsync("de", "translation", "greeting", "Hallo");
        var i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend)) { Language = "de-AT" };

        i18Next.T("greeting").ShouldBe("Hallo");

        await _backend.SetValueAsync("de", "translation", "greeting", "Servus");

        i18Next.T("greeting").ShouldBe("Servus");

        await _backend.RemoveValueAsync("de", "translation", "greeting");

        i18Next.T("greeting").ShouldBe("greeting");
    }

    [Fact]
    public void CacheExpiration_ShouldBeProvidedToTranslator()
    {
        _backend.CacheExpiration.ShouldBeNull();
        _backend.CacheExpiration = TimeSpan.FromMinutes(5);

        ((IExpiringTranslationBackend)_backend).CacheExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task ChainedBackend_SaveToEarlierBackends_ShouldStoreNamespace()
    {
        var source = new InMemoryBackend();
        source.AddTranslation("en", "translation", "greeting", "Hello");
        var chain = new ChainedBackend(_backend, source) { SaveToEarlierBackends = true };

        (await chain.LoadNamespaceAsync("en", "translation")).GetValue("greeting", null).ShouldBe("Hello");

        _database.GetEntries().ShouldHaveSingleItem().Value.ShouldBe("Hello");
    }
}
