using System.IO;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.TranslationTrees;

public class FlatTranslationTreeBuilderFixture
{
    [Fact]
    public void Build_KeysWithDots_ShouldKeepKeysVerbatim()
    {
        var builder = new FlatTranslationTreeBuilder { Namespace = "ns" };

        builder.AddTranslation("Hello. How are you?", "Hallo. Wie geht es dir?");
        builder.AddTranslation("Hello", "Hallo");

        var tree = builder.Build();

        tree.Namespace.ShouldBe("ns");
        tree.GetValue("Hello. How are you?", null).ShouldBe("Hallo. Wie geht es dir?");
        tree.GetValue("Hello", null).ShouldBe("Hallo");
        tree.GetValue("Hello. How", null).ShouldBeNull();
        tree.GetAllValues().Count().ShouldBe(2);
    }

    [Fact]
    public async Task JsonFileBackend_FlatBuilder_ShouldUseJoinedKeys()
    {
        var backend = new JsonFileBackend("TestFiles", new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());

        var tree = await backend.LoadNamespaceAsync("en-US", "test");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.GetValue("SectionB.SubSectionA.Value1", null).ShouldBe("Translated value 1");
        tree.GetValue("SectionB", null).ShouldBeNull();
    }
}
