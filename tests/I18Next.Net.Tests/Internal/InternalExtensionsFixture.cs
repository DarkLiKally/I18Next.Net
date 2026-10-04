using System;
using System.Collections.Generic;

using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Internal;

public class InternalExtensionsFixture
{
    [Fact]
    public void StringExtensions_ShouldSplitAndReplace()
    {
        "a-b-a".ReplaceFirst("a", "c").ShouldBe("c-b-a");
        "a-b".ReplaceFirst("x", "c").ShouldBe("a-b");
        "a::b::c".Split("::").ShouldBe(["a", "b", "c"]);
        "a::b::c".Split("::", 2).ShouldBe(["a", "b::c"]);
        "a::::c".Split("::", StringSplitOptions.RemoveEmptyEntries).ShouldBe(["a", "c"]);
        "a::b::c".Split("::", 2, StringSplitOptions.RemoveEmptyEntries).ShouldBe(["a", "b::c"]);
    }

    [Fact]
    public void ObjectExtensions_ShouldConvertObjectsToDictionaries()
    {
        var dictionary = new Dictionary<string, object> { ["a"] = 1 };

        ((object)null).ToDictionary().ShouldBeEmpty();
        dictionary.ToDictionary().ShouldBeSameAs(dictionary);
        new { a = 1, b = "x" }.ToDictionary().ShouldBeEquivalentTo(new Dictionary<string, object> { ["a"] = 1, ["b"] = "x" });
    }

    [Fact]
    public void DictionaryExtensions_MergeLeft_ShouldPreferLaterValues()
    {
        IDictionary<string, int> left = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        left.MergeLeft(new Dictionary<string, int> { ["b"] = 3, ["c"] = 4 })
            .ShouldBeEquivalentTo(new Dictionary<string, int> { ["a"] = 1, ["b"] = 3, ["c"] = 4 });
    }

    [Fact]
    public void Exceptions_ShouldExposeProperties()
    {
        var inner = new InvalidOperationException();

        new TranslationNamespaceNotFoundException("ns").Namespace.ShouldBe("ns");
        new TranslationNamespaceNotFoundException("ns", "message").Message.ShouldBe("message");
        new TranslationNamespaceNotFoundException("ns", "message", inner).InnerException.ShouldBeSameAs(inner);

        new TranslationKeyInvalidException("key").Key.ShouldBe("key");
        new TranslationKeyInvalidException("key", "message").Message.ShouldBe("message");
        new TranslationKeyInvalidException("key", "message", inner).InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void TimeZoneData_ShouldFindTimeZonesByOffset()
    {
        TimeZoneData.GetFirstForOffset(TimeSpan.FromHours(1)).Abbreviation.ShouldBe("A");
        TimeZoneData.GetFirstForOffset("Europe", TimeSpan.FromHours(1)).Abbreviation.ShouldBe("BST");
        TimeZoneData.GetFirstForOffset(TimeSpan.FromHours(30)).ShouldBeNull();
    }
}
