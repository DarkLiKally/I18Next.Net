using System;
using System.Collections.Generic;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;
using NUnit.Framework;

namespace I18Next.Net.Tests.Internal;

[TestFixture]
public class InternalExtensionsFixture
{
    [Test]
    public void StringExtensions_ShouldSplitAndReplace()
    {
        "a-b-a".ReplaceFirst("a", "c").Should().Be("c-b-a");
        "a-b".ReplaceFirst("x", "c").Should().Be("a-b");
        "a::b::c".Split("::").Should().Equal("a", "b", "c");
        "a::b::c".Split("::", 2).Should().Equal("a", "b::c");
        "a::::c".Split("::", StringSplitOptions.RemoveEmptyEntries).Should().Equal("a", "c");
        "a::b::c".Split("::", 2, StringSplitOptions.RemoveEmptyEntries).Should().Equal("a", "b::c");
    }

    [Test]
    public void ObjectExtensions_ShouldConvertObjectsToDictionaries()
    {
        var dictionary = new Dictionary<string, object> { ["a"] = 1 };

        ((object) null).ToDictionary().Should().BeEmpty();
        dictionary.ToDictionary().Should().BeSameAs(dictionary);
        new { a = 1, b = "x" }.ToDictionary().Should().BeEquivalentTo(new Dictionary<string, object> { ["a"] = 1, ["b"] = "x" });
    }

    [Test]
    public void DictionaryExtensions_MergeLeft_ShouldPreferLaterValues()
    {
        IDictionary<string, int> left = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        left.MergeLeft(new Dictionary<string, int> { ["b"] = 3, ["c"] = 4 })
            .Should().BeEquivalentTo(new Dictionary<string, int> { ["a"] = 1, ["b"] = 3, ["c"] = 4 });
    }

    [Test]
    public void Exceptions_ShouldExposeProperties()
    {
        var inner = new InvalidOperationException();

        new TranslationNamespaceNotFoundException("ns").Namespace.Should().Be("ns");
        new TranslationNamespaceNotFoundException("ns", "message").Message.Should().Be("message");
        new TranslationNamespaceNotFoundException("ns", "message", inner).InnerException.Should().BeSameAs(inner);

        new TranslationKeyInvalidException("key").Key.Should().Be("key");
        new TranslationKeyInvalidException("key", "message").Message.Should().Be("message");
        new TranslationKeyInvalidException("key", "message", inner).InnerException.Should().BeSameAs(inner);
    }

    [Test]
    public void TimeZoneData_ShouldFindTimeZonesByOffset()
    {
        TimeZoneData.GetFirstForOffset(TimeSpan.FromHours(1)).Abbreviation.Should().Be("A");
        TimeZoneData.GetFirstForOffset("Europe", TimeSpan.FromHours(1)).Abbreviation.Should().Be("BST");
        TimeZoneData.GetFirstForOffset(TimeSpan.FromHours(30)).Should().BeNull();
    }
}
