using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;
using NUnit.Framework;

namespace I18Next.Net.Tests.TranslationTrees;

[TestFixture]
public class FlatTranslationTreeBuilderFixture
{
    [Test]
    public void Build_KeysWithDots_ShouldKeepKeysVerbatim()
    {
        var builder = new FlatTranslationTreeBuilder { Namespace = "ns" };

        builder.AddTranslation("Hello. How are you?", "Hallo. Wie geht es dir?");
        builder.AddTranslation("Hello", "Hallo");

        var tree = builder.Build();

        tree.Namespace.Should().Be("ns");
        tree.GetValue("Hello. How are you?", null).Should().Be("Hallo. Wie geht es dir?");
        tree.GetValue("Hello", null).Should().Be("Hallo");
        tree.GetValue("Hello. How", null).Should().BeNull();
        tree.GetAllValues().Should().HaveCount(2);
    }

    [Test]
    public async Task JsonFileBackend_FlatBuilder_ShouldUseJoinedKeys()
    {
        var backend = new JsonFileBackend("TestFiles", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());

        var tree = await backend.LoadNamespaceAsync("en-US", "test");

        tree.Should().BeOfType<DictionaryTranslationTree>();
        tree.GetValue("SectionB.SubSectionA.Value1", null).Should().Be("Translated value 1");
        tree.GetValue("SectionB", null).Should().BeNull();
    }
}
