using System.Collections.Generic;
using System.Linq;
using I18Next.Net.TranslationTrees;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.TranslationTrees;

public class TranslationTreeFixture
{
    public TranslationTreeFixture()
    {
        var builder = new HierarchicalTranslationTreeBuilder();

        builder.AddTranslation("key", "value of key");
        builder.AddTranslation("look.deep", "value of look deep");
        builder.AddTranslation("look.deeper.down", "value of look deeper down");

        _tree = builder.Build();
    }
    private ITranslationTree _tree;


    [Fact]
    public void GetAllValues_NestedTranslations_ShouldReturnFullKeyPaths()
    {
        _tree.GetAllValues().ShouldBeEquivalentTo(new Dictionary<string, string>
        {
            ["key"] = "value of key",
            ["look.deep"] = "value of look deep",
            ["look.deeper.down"] = "value of look deeper down"
        });
    }

    [Fact]
    public void GetValue_ExistingKeys_ShouldReturnValues()
    {
        _tree.GetValue("key", null).ShouldBe("value of key");
        _tree.GetValue("look.deep", null).ShouldBe("value of look deep");
        _tree.GetValue("look.deeper.down", null).ShouldBe("value of look deeper down");
    }

    [Fact]
    public void GetValue_MissingKeys_ShouldReturnNull()
    {
        _tree.GetValue("missing", null).ShouldBeNull();
        _tree.GetValue("look.missing", null).ShouldBeNull();
        _tree.GetValue("look.deeper.missing", null).ShouldBeNull();
    }

    [Fact]
    public void GetValue_KeyLeadingToGroup_ShouldThrow()
    {
        Should.Throw<TranslationKeyInvalidException>(() => _tree.GetValue("look", null))
            .Key.ShouldBe("look");
    }

    [Fact]
    public void GetValue_KeyGoingBeyondTranslation_ShouldThrow()
    {
        Should.Throw<TranslationKeyInvalidException>(() => _tree.GetValue("key.sub", null))
            .Key.ShouldBe("key.sub");
    }

    [Fact]
    public void GetValue_ChangedTranslationValue_ShouldReturnNewValue()
    {
        var tree = (TranslationTree) _tree;
        var translation = (Translation) ((TranslationGroup) tree.Root).Children[0];

        translation.Value = "changed";

        tree.GetValue("key", null).ShouldBe("changed");
    }

    [Fact]
    public void Root_Replaced_ShouldUseNewRoot()
    {
        var tree = (TranslationTree) _tree;

        tree.Root = new TranslationGroup("", new TranslationTreeNode[] { new Translation("other", "other value") });

        tree.GetValue("other", null).ShouldBe("other value");
        tree.GetValue("key", null).ShouldBeNull();
        tree.GetAllValues().ShouldHaveSingleItem();
    }

    [Fact]
    public void TryGetChild_DuplicateNames_ShouldReturnFirstChild()
    {
        var group = new TranslationGroup("", new TranslationTreeNode[] { new Translation("a", "first"), new Translation("a", "second") });

        group.TryGetChild("a", out var child).ShouldBeTrue();
        ((Translation) child).Value.ShouldBe("first");
        group.TryGetChild("b", out _).ShouldBeFalse();
    }

    [Fact]
    public void DictionaryTranslationTree_ShouldProvideValues()
    {
        var tree = new DictionaryTranslationTree("ns", new Dictionary<string, string> { ["a"] = "b" });

        tree.AddValue("c", "d");
        tree["e"] = "f";

        tree.Namespace.ShouldBe("ns");
        tree.GetValue("a", null).ShouldBe("b");
        tree.GetValue("missing", null).ShouldBeNull();
        tree["c"].ShouldBe("d");
        tree.GetAllValues().Count().ShouldBe(3);
    }

    [Fact]
    public void GetGroupValues_ShouldReturnRelativeValues()
    {
        var tree = (IHierarchicalTranslationTree) _tree;

        tree.GetGroupValues("look").ShouldBeEquivalentTo(new Dictionary<string, string>
        {
            ["deep"] = "value of look deep",
            ["deeper.down"] = "value of look deeper down"
        });
        tree.GetGroupValues("look.deeper").ShouldContainKey("down");
        tree.GetGroupValues(null).Count().ShouldBe(3);
        tree.GetGroupValues("key").ShouldBeNull();
        tree.GetGroupValues("missing").ShouldBeNull();
        tree.GetGroupValues("key.sub").ShouldBeNull();
    }

    [Fact]
    public void DictionaryTranslationTree_GetGroupValues_ShouldFilterByPrefix()
    {
        var tree = new DictionaryTranslationTree("ns", new Dictionary<string, string> { ["a.b"] = "1", ["a.c.d"] = "2", ["ab"] = "3" });

        tree.GetGroupValues("a").ShouldBeEquivalentTo(new Dictionary<string, string> { ["b"] = "1", ["c.d"] = "2" });
        tree.GetGroupValues("").Count().ShouldBe(3);
        tree.GetGroupValues("missing").ShouldBeNull();
        new DictionaryTranslationTree("ns").GetGroupValues(null).ShouldBeNull();
    }
}
