using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;
using I18Next.Net.Yaml;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class YamlFileBackendFixture
{
    private readonly YamlFileBackend _backend = new(Path.Combine("TestFiles", "yaml"));

    [Fact]
    public async Task LoadNamespaceAsync_ShouldReadScalarsMappingsAndSequences()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "translation");

        tree.Namespace.ShouldBe("translation");
        tree.GetValue("welcome", null).ShouldBe("Hello {{name}}!");
        tree.GetValue("menu.title", null).ShouldBe("Menu");
        tree.GetValue("menu.items.1", null).ShouldBe("About {{name}}");
        tree.GetValue("menu.links.0.url", null).ShouldBe("/privacy");
        tree.GetValue("multiline", null).ShouldBe("First line\nSecond line\n");
        tree.GetValue("folded", null).ShouldBe("Folded text\n");
        tree.GetValue("quoted", null).ShouldBe("It's quoted");
        tree.GetValue("number", null).ShouldBe("42");
        tree.GetValue("flag", null).ShouldBe("true");
        tree.GetValue("quotedNull", null).ShouldBe("null");
        tree.GetValue("copy.color", null).ShouldBe("Red");
    }

    [Fact]
    public async Task LoadNamespaceAsync_NullValues_ShouldBeSkipped()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "translation");

        tree.GetValue("empty", null).ShouldBeNull();
        tree.GetValue("nothing", null).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_YmlExtensionAndLanguagePart_ShouldBeFound()
    {
        (await _backend.LoadNamespaceAsync("en-GB", "common")).GetValue("save", null).ShouldBe("Save");
        (await _backend.LoadNamespaceAsync("fr", "common")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_EmptyOrNonMappingDocument_ShouldReturnEmptyTree()
    {
        (await _backend.LoadNamespaceAsync("de", "empty")).GetValue("anything", null).ShouldBeNull();
        (await _backend.LoadNamespaceAsync("de", "list")).GetValue("0", null).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_FlatTreeBuilder_ShouldBeUsed()
    {
        var backend = new YamlFileBackend(Path.Combine("TestFiles", "yaml"), new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.GetValue("menu.title", null).ShouldBe("Menu");
    }

    [Fact]
    public void Constructors_ShouldUseLocalesByDefault()
    {
        new YamlFileBackend().ShouldNotBeNull();
        new YamlFileBackend(new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>()).ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_OtherEncoding_ShouldBeUsed()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(directory, "de"));

        try
        {
            File.WriteAllText(Path.Combine(directory, "de", "translation.yaml"), "text: Grüße", Encoding.GetEncoding("iso-8859-1"));

            var backend = new YamlFileBackend(directory) { Encoding = Encoding.GetEncoding("iso-8859-1") };

            (await backend.LoadNamespaceAsync("de", "translation")).GetValue("text", null).ShouldBe("Grüße");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Read_String_ShouldUseHierarchicalTree()
    {
        var tree = YamlTranslationReader.Read("a:\n  b: c", "ns");

        tree.Namespace.ShouldBe("ns");
        tree.GetValue("a.b", null).ShouldBe("c");
    }

    [Fact]
    public async Task I18Next_WithYamlBackend_ShouldTranslate()
    {
        var translator = new DefaultTranslator(_backend, new TraceLogger(), new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
            new DefaultInterpolator(new TraceLogger()));
        var i18Next = new I18NextNet(_backend, translator) { Language = "de-AT" };

        (await i18Next.Ta("welcome", new { name = "Anna" })).ShouldBe("Hallo Anna!");
        (await i18Next.Ta("item", new { count = 3 })).ShouldBe("3 Elemente");
        (await i18Next.Ta<string[]>("en", "menu.items", new { name = "Anna" })).ShouldBe(["Home", "About Anna"]);
    }

    [Fact]
    public async Task HttpBackend_WithYamlParser_ShouldTranslate()
    {
        var handler = new YamlHandler();
        var backend = new HttpBackend(new HttpClient(handler) { BaseAddress = new System.Uri("https://cdn.example.com/") }, "locales/{{lng}}/{{ns}}.yaml")
        {
            Parse = YamlTranslationReader.Read
        };

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.Namespace.ShouldBe("translation");
        tree.GetValue("greeting", null).ShouldBe("Hi");
    }

    [Fact]
    public async Task FuncBackend_WithYamlReader_ShouldTranslate()
    {
        var backend = new FuncBackend((language, ns) => language == "en" ? YamlTranslationReader.Read("greeting: Hi", ns) : null);

        (await backend.LoadNamespaceAsync("en-US", "translation")).GetValue("greeting", null).ShouldBe("Hi");
    }

    private class YamlHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("greeting: Hi") });
        }
    }
}
