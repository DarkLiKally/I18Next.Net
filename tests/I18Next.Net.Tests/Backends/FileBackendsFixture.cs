using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;
using NUnit.Framework;

namespace I18Next.Net.Tests.Backends;

[TestFixture]
public class FileBackendsFixture
{
    [Test]
    public async Task LoadNamespaceAsync_MissingFiles_ShouldReturnNull()
    {
        (await new JsonFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).Should().BeNull();
        (await new XmlFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).Should().BeNull();
        (await new StrictXmlFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).Should().BeNull();
        (await new IniFileBackend("TestFiles").LoadNamespaceAsync("fr", "test")).Should().BeNull();
    }

    [Test]
    public async Task LoadNamespaceAsync_DefaultBasePath_ShouldUseLocalesDirectory()
    {
        (await new JsonFileBackend().LoadNamespaceAsync("en", "test")).Should().BeNull();
        (await new XmlFileBackend().LoadNamespaceAsync("en", "test")).Should().BeNull();
        (await new StrictXmlFileBackend().LoadNamespaceAsync("en", "test")).Should().BeNull();
        (await new IniFileBackend().LoadNamespaceAsync("en", "test")).Should().BeNull();
    }

    [Test]
    public async Task LoadNamespaceAsync_CustomTreeBuilderFactory_ShouldUseFactory()
    {
        var factory = new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>();

        (await new JsonFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).Should().Be("Translated value 1");
        (await new XmlFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).Should().Be("Translated value 1");
        (await new IniFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test")).GetValue("Value1", null).Should().Be("Translated value 1");
        (await new StrictXmlFileBackend("TestFiles", factory).LoadNamespaceAsync("en-US", "test-strict")).GetValue("Value1", null).Should().Be("Translated value 1");
    }

    [Test]
    public async Task JsonFileBackend_DifferentValueTypes_ShouldBeConvertedToStrings()
    {
        var tree = await new JsonFileBackend("TestFiles").LoadNamespaceAsync("en-US", "types");

        tree.GetValue("String", null).Should().Be("Text");
        tree.GetValue("Integer", null).Should().Be("42");
        tree.GetValue("Decimal", null).Should().Be("1.50");
        tree.GetValue("True", null).Should().Be("True");
        tree.GetValue("False", null).Should().Be("False");
        tree.GetValue("Null", null).Should().BeNull();
        tree.GetValue("Date", null).Should().Be("2018-01-25T07:37:59Z");
        tree.GetValue("Array", null).Should().BeNull();
        tree.GetValue("Nested.Value", null).Should().Be("Nested text");
    }

    [Test]
    public async Task JsonFileBackend_NonUtf8Encoding_ShouldReadFile()
    {
        var backend = new JsonFileBackend("TestFiles") { Encoding = Encoding.Unicode };
        var directory = Path.Combine("TestFiles", "utf16");

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "test.json"), "{ \"Key\": \"Wert äöü\" }", Encoding.Unicode);

        var tree = await backend.LoadNamespaceAsync("utf16", "test");

        tree.GetValue("Key", null).Should().Be("Wert äöü");
    }

    [Test]
    public async Task JsonFileBackend_OverriddenFindFile_ShouldUseCustomFileLayout()
    {
        var backend = new CustomJsonFileBackend(Path.Combine("TestFiles", "custom"));

        var tree = await backend.LoadNamespaceAsync("en-US", "test");

        tree.Should().NotBeNull();
        tree.GetValue("Value1", null).Should().Be("Custom value 1");
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
