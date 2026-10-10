using System;
using System.Collections.Generic;
using System.Linq;

using I18Next.Net.Tool.Resources;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Resources;

public class TranslationDocumentFixture
{
    private const string Json = """
                                {
                                  // comment
                                  "greeting": "Hello {{name}}",
                                  "menu": { "title": "Menu", "items": [ "Home", { "label": "About" } ] },
                                  "flat.key": "Flat",
                                  "count": 3,
                                  "flag": true,
                                  "nothing": null,
                                }
                                """;

    [Fact]
    public void GetEntries_ShouldFlattenObjectsAndArraysInOrder()
    {
        var document = TranslationDocument.Parse(Json, ".");

        document.GetEntries().ShouldBe([
            new KeyValuePair<string, string>("greeting", "Hello {{name}}"),
            new KeyValuePair<string, string>("menu.title", "Menu"),
            new KeyValuePair<string, string>("menu.items.0", "Home"),
            new KeyValuePair<string, string>("menu.items.1.label", "About"),
            new KeyValuePair<string, string>("flat.key", "Flat"),
            new KeyValuePair<string, string>("count", "3"),
            new KeyValuePair<string, string>("flag", "True")
        ]);
    }

    [Fact]
    public void GetValue_ShouldFindNestedFlatAndArrayKeys()
    {
        var document = TranslationDocument.Parse(Json, ".");

        document.GetValue("menu.title").ShouldBe("Menu");
        document.GetValue("flat.key").ShouldBe("Flat");
        document.GetValue("menu.items.1.label").ShouldBe("About");
        document.GetValue("menu.items.5").ShouldBeNull();
        document.GetValue("menu.items.x").ShouldBeNull();
        document.GetValue("menu").ShouldBeNull();
        document.GetValue("greeting.child").ShouldBeNull();
        document.ContainsKey("menu").ShouldBeTrue();
        document.ContainsKey("menu.missing").ShouldBeFalse();
        document.ContainsKey("nothing").ShouldBeFalse();
    }

    [Fact]
    public void SetValue_ShouldCreateNestedObjects()
    {
        var document = new TranslationDocument(".");

        document.SetValue("a.b.c", "1").ShouldBeTrue();
        document.SetValue("a.d", "2").ShouldBeTrue();
        document.SetValue("e", "3").ShouldBeTrue();

        document.ToJson().ShouldBe("{\n  \"a\": {\n    \"b\": {\n      \"c\": \"1\"\n    },\n    \"d\": \"2\"\n  },\n  \"e\": \"3\"\n}\n");
    }

    [Fact]
    public void SetValue_ExistingFlatKeyOrBlockedPath_ShouldSetAFlatKey()
    {
        var document = TranslationDocument.Parse("""{ "a.b": "old", "c": "value" }""", ".");

        document.SetValue("a.b", "new").ShouldBeTrue();
        document.SetValue("c.d", "child").ShouldBeTrue();

        document.Root.ToJsonString().ShouldBe("""{"a.b":"new","c":"value","c.d":"child"}""");
    }

    [Fact]
    public void SetValue_Arrays_ShouldOnlyReplaceExistingItems()
    {
        var document = TranslationDocument.Parse("""{ "list": [ "a", { "b": "c" }, "d" ] }""", ".");

        document.SetValue("list.0", "x").ShouldBeTrue();
        document.SetValue("list.1.b", "y").ShouldBeTrue();
        document.SetValue("list.5", "z").ShouldBeFalse();
        document.SetValue("list.2.e", "z").ShouldBeFalse();

        document.Root.ToJsonString().ShouldBe("""{"list":["x",{"b":"y"},"d"]}""");
    }

    [Fact]
    public void FlatSeparator_ShouldUseKeysAsTheyAre()
    {
        var document = TranslationDocument.FromEntries([new KeyValuePair<string, string>("a.b", "1")], "");

        document.Root.ToJsonString().ShouldBe("""{"a.b":"1"}""");
        document.GetValue("a.b").ShouldBe("1");
        document.Remove("a").ShouldBeFalse();
    }

    [Fact]
    public void Remove_ShouldRemoveEmptyParents()
    {
        var document = TranslationDocument.Parse("""{ "a": { "b": { "c": "1" }, "d": "2" }, "list": [ { "x": "y" } ], "e.f": "3" }""", ".");

        document.Remove("a.b.c").ShouldBeTrue();
        document.Remove("e.f").ShouldBeTrue();
        document.Remove("list.0.x").ShouldBeTrue();
        document.Remove("missing.key").ShouldBeFalse();
        document.Remove("list.0").ShouldBeFalse();

        document.Root.ToJsonString().ShouldBe("""{"a":{"d":"2"},"list":[{}]}""");

        document.Remove("a.d").ShouldBeTrue();
        document.Root.ToJsonString().ShouldBe("""{"list":[{}]}""");
    }

    [Fact]
    public void Sort_ShouldSortObjectsRecursivelyAndKeepArrays()
    {
        var document = TranslationDocument.Parse("""{ "b": { "z": "1", "a": "2" }, "B": "3", "a": [ { "y": "1", "x": "2" }, "c", "b" ] }""", ".");

        document.Sort();

        document.Root.ToJsonString().ShouldBe("""{"B":"3","a":[{"x":"2","y":"1"},"c","b"],"b":{"a":"2","z":"1"}}""");
    }

    [Fact]
    public void ToJson_ShouldNotEscapeUnicodeOrHtml()
    {
        var document = TranslationDocument.Parse("""{ "text": "Grüße <b>&</b> 'x' \"y\" é" }""", ".");

        document.ToJson().ShouldBe("{\n  \"text\": \"Grüße <b>&</b> 'x' \\\"y\\\" é\"\n}\n");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("{ invalid")]
    [InlineData("""{ "a": "1", "a": "2" }""")]
    public void Parse_InvalidJson_ShouldThrowFormatException(string json)
    {
        Should.Throw<FormatException>(() => TranslationDocument.Parse(json, "."));
    }

    [Fact]
    public void Parse_EmptyText_ShouldCreateAnEmptyDocument()
    {
        TranslationDocument.Parse("  ", ".").GetEntries().ShouldBeEmpty();
        TranslationDocument.Parse("{}", null).KeySeparator.ShouldBe(string.Empty);
    }

    [Fact]
    public void FromEntries_ShouldNestKeys()
    {
        var document = TranslationDocument.FromEntries([
            new KeyValuePair<string, string>("Home.Title", "Title"),
            new KeyValuePair<string, string>("Home.Body", "Body"),
            new KeyValuePair<string, string>("Plain", "Plain")
        ], ".");

        document.GetEntries().Select(e => e.Key).ShouldBe(["Home.Title", "Home.Body", "Plain"]);
        document.Root["Home"]!["Title"]!.GetValue<string>().ShouldBe("Title");
    }
}
