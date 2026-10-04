using System.Collections.Generic;
using FluentAssertions;
using I18Next.Net.TranslationTrees;
using NUnit.Framework;

namespace I18Next.Net.Tests.TranslationTrees;

[TestFixture]
public class TranslationTreeFixture
{
    private ITranslationTree _tree;

    [SetUp]
    public void SetUp()
    {
        var builder = new HierarchicalTranslationTreeBuilder();

        builder.AddTranslation("key", "value of key");
        builder.AddTranslation("look.deep", "value of look deep");
        builder.AddTranslation("look.deeper.down", "value of look deeper down");

        _tree = builder.Build();
    }

    [Test]
    public void GetAllValues_NestedTranslations_ShouldReturnFullKeyPaths()
    {
        _tree.GetAllValues().Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["key"] = "value of key",
            ["look.deep"] = "value of look deep",
            ["look.deeper.down"] = "value of look deeper down"
        });
    }

    [Test]
    public void GetValue_ExistingKeys_ShouldReturnValues()
    {
        _tree.GetValue("key", null).Should().Be("value of key");
        _tree.GetValue("look.deep", null).Should().Be("value of look deep");
        _tree.GetValue("look.deeper.down", null).Should().Be("value of look deeper down");
    }

    [Test]
    public void GetValue_MissingKeys_ShouldReturnNull()
    {
        _tree.GetValue("missing", null).Should().BeNull();
        _tree.GetValue("look.missing", null).Should().BeNull();
        _tree.GetValue("look.deeper.missing", null).Should().BeNull();
    }

    [Test]
    public void GetValue_KeyLeadingToGroup_ShouldThrow()
    {
        _tree.Invoking(t => t.GetValue("look", null)).Should().Throw<TranslationKeyInvalidException>()
            .Which.Key.Should().Be("look");
    }

    [Test]
    public void GetValue_KeyGoingBeyondTranslation_ShouldThrow()
    {
        _tree.Invoking(t => t.GetValue("key.sub", null)).Should().Throw<TranslationKeyInvalidException>()
            .Which.Key.Should().Be("key.sub");
    }

    [Test]
    public void GetValue_ChangedTranslationValue_ShouldReturnNewValue()
    {
        var tree = (TranslationTree) _tree;
        var translation = (Translation) ((TranslationGroup) tree.Root).Children[0];

        translation.Value = "changed";

        tree.GetValue("key", null).Should().Be("changed");
    }

    [Test]
    public void Root_Replaced_ShouldUseNewRoot()
    {
        var tree = (TranslationTree) _tree;

        tree.Root = new TranslationGroup("", new TranslationTreeNode[] { new Translation("other", "other value") });

        tree.GetValue("other", null).Should().Be("other value");
        tree.GetValue("key", null).Should().BeNull();
        tree.GetAllValues().Should().ContainSingle();
    }

    [Test]
    public void TryGetChild_DuplicateNames_ShouldReturnFirstChild()
    {
        var group = new TranslationGroup("", new TranslationTreeNode[] { new Translation("a", "first"), new Translation("a", "second") });

        group.TryGetChild("a", out var child).Should().BeTrue();
        ((Translation) child).Value.Should().Be("first");
        group.TryGetChild("b", out _).Should().BeFalse();
    }

    [Test]
    public void DictionaryTranslationTree_ShouldProvideValues()
    {
        var tree = new DictionaryTranslationTree("ns", new Dictionary<string, string> { ["a"] = "b" });

        tree.AddValue("c", "d");
        tree["e"] = "f";

        tree.Namespace.Should().Be("ns");
        tree.GetValue("a", null).Should().Be("b");
        tree.GetValue("missing", null).Should().BeNull();
        tree["c"].Should().Be("d");
        tree.GetAllValues().Should().HaveCount(3);
    }

    [Test]
    public void GetGroupValues_ShouldReturnRelativeValues()
    {
        var tree = (IHierarchicalTranslationTree) _tree;

        tree.GetGroupValues("look").Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["deep"] = "value of look deep",
            ["deeper.down"] = "value of look deeper down"
        });
        tree.GetGroupValues("look.deeper").Should().ContainKey("down");
        tree.GetGroupValues(null).Should().HaveCount(3);
        tree.GetGroupValues("key").Should().BeNull();
        tree.GetGroupValues("missing").Should().BeNull();
        tree.GetGroupValues("key.sub").Should().BeNull();
    }

    [Test]
    public void DictionaryTranslationTree_GetGroupValues_ShouldFilterByPrefix()
    {
        var tree = new DictionaryTranslationTree("ns", new Dictionary<string, string> { ["a.b"] = "1", ["a.c.d"] = "2", ["ab"] = "3" });

        tree.GetGroupValues("a").Should().BeEquivalentTo(new Dictionary<string, string> { ["b"] = "1", ["c.d"] = "2" });
        tree.GetGroupValues("").Should().HaveCount(3);
        tree.GetGroupValues("missing").Should().BeNull();
        new DictionaryTranslationTree("ns").GetGroupValues(null).Should().BeNull();
    }
}
