using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class FuncBackendFixture
{
    private const string Json = """{ "greeting": "Hello {{name}}", "nested": { "key": "Nested" }, "list": [ "a", "b" ] }""";

    [Fact]
    public async Task Constructor_TreeLoader_ShouldReturnTree()
    {
        var tree = new DictionaryTranslationTree("translation") { ["key"] = "value" };
        var backend = new FuncBackend((language, ns) => language == "en" ? tree : null);

        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeSameAs(tree);
        (await backend.LoadNamespaceAsync("en-GB", "translation")).ShouldBeSameAs(tree);
        (await backend.LoadNamespaceAsync("de", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task Constructor_AsyncTreeLoader_ShouldReturnTree()
    {
        var backend = new FuncBackend(async (language, ns) =>
        {
            await Task.Yield();

            return new DictionaryTranslationTree(ns) { ["key"] = language };
        });

        (await backend.LoadNamespaceAsync("de", "common")).GetValue("key", null).ShouldBe("de");
    }

    [Fact]
    public async Task LoadNamespaceAsync_FallbackDisabled_ShouldNotLoadLanguagePart()
    {
        var requested = new List<string>();
        var backend = new FuncBackend((language, ns) =>
        {
            requested.Add(language);

            return (ITranslationTree)null;
        }) { FallbackToLanguagePart = false };

        (await backend.LoadNamespaceAsync("de-AT", "translation")).ShouldBeNull();
        requested.ShouldBe(["de-AT"]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_RegionalLanguageMissing_ShouldLoadLanguagePart()
    {
        var requested = new List<string>();
        var backend = FuncBackend.FromJson((language, ns) =>
        {
            requested.Add(language);

            return language == "de" ? """{ "key": "Wert" }""" : null;
        });

        (await backend.LoadNamespaceAsync("de-AT", "translation")).GetValue("key", null).ShouldBe("Wert");
        requested.ShouldBe(["de-AT", "de"]);
    }

    [Fact]
    public async Task FromJson_ShouldParseJson()
    {
        var backend = FuncBackend.FromJson((language, ns) => Json);

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.Namespace.ShouldBe("translation");
        tree.GetValue("greeting", null).ShouldBe("Hello {{name}}");
        tree.GetValue("nested.key", null).ShouldBe("Nested");
        tree.GetValue("list.1", null).ShouldBe("b");
    }

    [Fact]
    public async Task FromJson_Async_ShouldParseJson()
    {
        var backend = FuncBackend.FromJson(async (language, ns) =>
        {
            await Task.Yield();

            return ns == "translation" ? Json : null;
        });

        (await backend.LoadNamespaceAsync("en", "translation")).GetValue("nested.key", null).ShouldBe("Nested");
        (await backend.LoadNamespaceAsync("en", "missing")).ShouldBeNull();
    }

    [Fact]
    public async Task FromJson_TreeBuilderFactory_ShouldBeUsed()
    {
        var backend = FuncBackend.FromJson((language, ns) => Json, new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.Namespace.ShouldBe("translation");
    }

    [Fact]
    public async Task FromStream_ShouldParseAndDisposeStream()
    {
        var stream = new TrackingStream(Encoding.UTF8.GetBytes(Json));
        var backend = FuncBackend.FromStream((language, ns) => language == "en" ? stream : null);

        (await backend.LoadNamespaceAsync("en", "translation")).GetValue("greeting", null).ShouldBe("Hello {{name}}");
        (await backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
        stream.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task FromStream_Async_ShouldParseStream()
    {
        var backend = FuncBackend.FromStream(async (language, ns) =>
        {
            await Task.Yield();

            return new MemoryStream(Encoding.UTF8.GetBytes(Json));
        });

        (await backend.LoadNamespaceAsync("en", "translation")).GetValue("list.0", null).ShouldBe("a");
    }

    [Fact]
    public async Task FromObject_NestedDictionaries_ShouldBuildTree()
    {
        var backend = FuncBackend.FromObject((language, ns) => new Dictionary<string, object>
        {
            ["title"] = "Title",
            ["menu"] = new Dictionary<string, string> { ["home"] = "Home" },
            ["items"] = new List<object> { "first", new Dictionary<string, object> { ["label"] = "second" } },
            ["count"] = 1.5,
            ["enabled"] = true,
            ["empty"] = null,
            ["legacy"] = new Hashtable { [1] = "one" }
        });

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.GetValue("title", null).ShouldBe("Title");
        tree.GetValue("menu.home", null).ShouldBe("Home");
        tree.GetValue("items.0", null).ShouldBe("first");
        tree.GetValue("items.1.label", null).ShouldBe("second");
        tree.GetValue("count", null).ShouldBe("1.5");
        tree.GetValue("enabled", null).ShouldBe("True");
        tree.GetValue("empty", null).ShouldBeNull();
        tree.GetValue("legacy.1", null).ShouldBe("one");
    }

    [Fact]
    public async Task FromObject_AnonymousObject_ShouldBuildTree()
    {
        var backend = FuncBackend.FromObject((language, ns) => new
        {
            greeting = "Hello",
            nested = new { key = "Nested", values = new[] { "x", "y" } }
        });

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.GetValue("greeting", null).ShouldBe("Hello");
        tree.GetValue("nested.key", null).ShouldBe("Nested");
        tree.GetValue("nested.values.1", null).ShouldBe("y");
    }

    [Fact]
    public async Task FromObject_StringAndHashtableRoots_ShouldBuildTree()
    {
        var flat = FuncBackend.FromObject((language, ns) => new Dictionary<string, string> { ["a.b"] = "flat" });
        var hashtable = FuncBackend.FromObject((language, ns) => new Hashtable { ["key"] = "value" });

        (await flat.LoadNamespaceAsync("en", "translation")).GetValue("a.b", null).ShouldBe("flat");
        (await hashtable.LoadNamespaceAsync("en", "translation")).GetValue("key", null).ShouldBe("value");
    }

    [Fact]
    public async Task FromObject_JsonElement_ShouldBuildTree()
    {
        using var document = JsonDocument.Parse("""{ "greeting": "Hi", "nested": { "list": [ 1, 2 ] } }""");
        var root = document.RootElement.Clone();
        var backend = FuncBackend.FromObject(async (language, ns) =>
        {
            await Task.Yield();

            return language == "en" ? (object)root : null;
        });

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.GetValue("greeting", null).ShouldBe("Hi");
        tree.GetValue("nested.list.1", null).ShouldBe("2");
        (await backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
    }

    [Fact]
    public void Factories_NullLoader_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new FuncBackend((Func<string, string, ITranslationTree>)null));
        Should.Throw<ArgumentNullException>(() => new FuncBackend((Func<string, string, Task<ITranslationTree>>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromJson((Func<string, string, string>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromJson((Func<string, string, Task<string>>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromStream((Func<string, string, Stream>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromStream((Func<string, string, Task<Stream>>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromObject((Func<string, string, object>)null));
        Should.Throw<ArgumentNullException>(() => FuncBackend.FromObject((Func<string, string, Task<object>>)null));
    }

    [Fact]
    public async Task I18Next_WithFuncBackend_ShouldTranslate()
    {
        var resources = new Dictionary<string, object>
        {
            ["en"] = new { translation = new { greeting = "Hello {{name}}" } },
            ["de"] = new { translation = new { greeting = "Hallo {{name}}" } }
        };
        var backend = FuncBackend.FromObject((language, ns) =>
            resources.TryGetValue(language, out var namespaces) ? namespaces.GetType().GetProperty(ns)?.GetValue(namespaces) : null);

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de-CH" };

        (await i18Next.Ta("greeting", new { name = "Anna" })).ShouldBe("Hallo Anna");
        (await i18Next.Ta("en", "greeting", new { name = "Anna" })).ShouldBe("Hello Anna");
    }

    private class TrackingStream(byte[] buffer) : MemoryStream(buffer)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
