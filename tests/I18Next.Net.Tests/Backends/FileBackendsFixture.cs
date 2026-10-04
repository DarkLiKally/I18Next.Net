using System.IO;
using System.Text;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Backends;

public class FileBackendsFixture
{
    [Fact]
    public async Task LoadNamespaceAsync_MissingFiles_ShouldReturnNull()
    {
        (await new JsonFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).ShouldBeNull();
        (await new XmlFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).ShouldBeNull();
        (await new StrictXmlFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).ShouldBeNull();
        (await new IniFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_DefaultBasePath_ShouldUseLocalesDirectory()
    {
        (await new JsonFileBackend().LoadNamespaceAsync("en", "test")).ShouldBeNull();
        (await new XmlFileBackend().LoadNamespaceAsync("en", "test")).ShouldBeNull();
        (await new StrictXmlFileBackend().LoadNamespaceAsync("en", "test")).ShouldBeNull();
        (await new IniFileBackend().LoadNamespaceAsync("en", "test")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_CustomTreeBuilderFactory_ShouldUseFactory()
    {
        var factory = new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>();

        (await new JsonFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).ShouldBe("Translated value 1");
        (await new XmlFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).ShouldBe("Translated value 1");
        (await new IniFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).ShouldBe("Translated value 1");
        (await new StrictXmlFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test-strict")).GetValue("Value1", null).ShouldBe("Translated value 1");
    }

    [Fact]
    public async Task JsonFileBackend_DifferentValueTypes_ShouldBeConvertedToStrings()
    {
        var tree = await new JsonFileBackend("TestFiles").LoadNamespaceAsync("en-US", "types");

        tree.GetValue("String", null).ShouldBe("Text");
        tree.GetValue("Integer", null).ShouldBe("42");
        tree.GetValue("Decimal", null).ShouldBe("1.50");
        tree.GetValue("True", null).ShouldBe("True");
        tree.GetValue("False", null).ShouldBe("False");
        tree.GetValue("Null", null).ShouldBeNull();
        tree.GetValue("Date", null).ShouldBe("2018-01-25T07:37:59Z");
        tree.GetValue("Array.0", null).ShouldBe("a");
        tree.GetValue("Array.1", null).ShouldBe("b");
        tree.GetValue("Nested.Value", null).ShouldBe("Nested text");
    }

    [Fact]
    public async Task JsonFileBackend_NonUtf8Encoding_ShouldReadFile()
    {
        var backend = new JsonFileBackend("TestFiles") { Encoding = Encoding.Unicode };
        var directory = Path.Combine("TestFiles", "utf16");

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "test.json"), "{ \"Key\": \"Wert äöü\" }", Encoding.Unicode);

        var tree = await backend.LoadNamespaceAsync("utf16", "test");

        tree.GetValue("Key", null).ShouldBe("Wert äöü");
    }

    [Fact]
    public async Task JsonFileBackend_OverriddenFindFile_ShouldUseCustomFileLayout()
    {
        var backend = new CustomJsonFileBackend(Path.Combine("TestFiles", "custom"));

        var tree = await backend.LoadNamespaceAsync("en-US", "test");

        tree.ShouldNotBeNull();
        tree.GetValue("Value1", null).ShouldBe("Custom value 1");
    }

    private class CustomJsonFileBackend : JsonFileBackend
    {
        public CustomJsonFileBackend(string basePath)
            : base(basePath)
        {
        }

        protected override string FindFile(string language, string @namespace)
        {
            var path = Path.Combine(BasePath, $"{@namespace}_{language}.json");

            if (File.Exists(path))
                return path;

            path = Path.Combine(BasePath, $"{@namespace}_{BackendUtilities.GetLanguagePart(language)}.json");

            return File.Exists(path) ? path : null;
        }
    }
}
