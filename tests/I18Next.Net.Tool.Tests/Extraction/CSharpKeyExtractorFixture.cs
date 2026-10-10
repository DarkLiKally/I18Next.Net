using System.Collections.Generic;
using System.Linq;

using I18Next.Net.Tool.Extraction;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Extraction;

public class CSharpKeyExtractorFixture
{
    private readonly ExtractionOptions _options = new();

    [Fact]
    public void Extract_TranslationMethods_ShouldFindTheKeys()
    {
        var keys = Extract("""
                           i18n.T("a");
                           i18n.Ta("b", new { name });
                           i18n.TObject("c");
                           await i18n.TaObject("d");
                           i18n.T<Menu>("e");
                           await i18n.Ta<Menu>("f");
                           i18n.Exists("g");
                           await i18n.ExistsAsync("de", "h");
                           i18n?.T("i");
                           T("j");
                           i18n.T(@"k");
                           i18n.T($"l");
                           i18n.T("m" + "." + "n");
                           i18n.T(("o"));
                           i18n.T(key: "p", args: new { count = 1 });
                           """);

        keys.Select(k => k.Key).ShouldBe(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m.n", "o", "p"]);
        keys.Select(k => k.ReturnsObject).ShouldBe([false, false, true, true, true, true, false, false, false, false, false, false, false, false, false]);
        keys.ShouldAllBe(k => k.Namespace == "translation" && k.File == "File.cs");
        keys[0].Line.ShouldBe(9);
        keys.Last().HasCount.ShouldBeTrue();
    }

    [Fact]
    public void Extract_LanguageAndNamespaceArguments_ShouldBeRecognized()
    {
        var keys = Extract("""
                           i18n.T("de", "a");
                           i18n.T(language, "b", new { count = 2 });
                           i18n.T("de", "common", "c");
                           i18n.T("de", "common", "d", new { });
                           i18n.T(language: "de", defaultNamespace: "common", key: "e");
                           i18n.T("f", args);
                           """);

        keys.Select(k => (k.Namespace, k.Key, k.HasCount)).ShouldBe([
            ("translation", "a", false),
            ("translation", "b", true),
            ("common", "c", false),
            ("common", "d", false),
            ("common", "e", false),
            ("translation", "f", false)
        ]);
    }

    [Fact]
    public void Extract_DynamicKeys_ShouldBeSkipped()
    {
        Extract("""
                i18n.T(key);
                i18n.T($"prefix.{name}");
                i18n.T("a" + name);
                i18n.T(GetKey());
                i18n.T();
                i18n.T(new string[0]);
                i18n.T(new[] { name });
                i18n.T(language, key);
                Other("x");
                """).ShouldBeEmpty();
    }

    [Fact]
    public void Extract_KeyArrays_ShouldUseTheLastKey()
    {
        Extract("""
                i18n.T(new[] { "specific", "fallback" });
                i18n.T(new string[] { "a", "b" });
                i18n.T(["c", "d"]);
                """).Select(k => k.Key).ShouldBe(["fallback", "b", "d"]);
    }

    [Fact]
    public void Extract_NamespacePrefixAndArguments_ShouldBeApplied()
    {
        var keys = Extract("""
                           i18n.T("common:save");
                           i18n.T("friend", new { context = "male", count = n });
                           i18n.T("place", new { count, ordinal = true });
                           i18n.T("title", new { keyPrefix = "home" });
                           i18n.T("items", new Dictionary<string, object> { ["count"] = 2, ["context"] = "x" });
                           i18n.T("rows", new Dictionary<string, object> { { "count", 2 } });
                           i18n.T("rank", new { ordinal = false, count = 1 });
                           """);

        keys.Select(k => (k.Namespace, k.Key, k.HasCount, k.Ordinal, k.Context)).ShouldBe([
            ("common", "save", false, false, null),
            ("translation", "friend", true, false, "male"),
            ("translation", "place", true, true, null),
            ("translation", "home.title", false, false, null),
            ("translation", "items", true, false, "x"),
            ("translation", "rows", true, false, null),
            ("translation", "rank", true, false, null)
        ]);
    }

    [Fact]
    public void Extract_FixedT_ShouldApplyNamespaceAndKeyPrefix()
    {
        var keys = Extract("""
                           var t = i18n.GetFixedT("de", "common", "home");
                           t.T("title");
                           t.T("other:key");
                           var named = i18n.GetFixedT(@namespace: "admin");
                           named.T("users", new { count = 1 });
                           _prefixed = i18n.GetFixedT(keyPrefix: "menu");
                           this._prefixed.T("open");
                           var created = new FixedT(i18n, null, "shop", "cart");
                           created.T("empty");
                           var stat = FixedTExtensions.GetFixedT(i18n, null, "static");
                           stat.T("x");
                           """, """
                                private FixedT Implicit { get; } = new(i18n, null, "implicit");
                                private FixedT _field = new(i18n, "de", "field", "p");
                                """, """
                                     Implicit.T("a");
                                     _field.T("b");
                                     """);

        keys.Select(k => (k.Namespace, k.Key, k.HasCount)).ShouldBe([
            ("common", "home.title", false),
            ("other", "home.key", false),
            ("admin", "users", true),
            ("translation", "menu.open", false),
            ("shop", "cart.empty", false),
            ("static", "x", false),
            ("implicit", "a", false),
            ("field", "p.b", false)
        ]);
    }

    [Fact]
    public void Extract_Localizers_ShouldFindIndexerKeys()
    {
        _options.LocalizerNames.Add("L");

        var keys = Extract("""
                           _ = localizer["a"];
                           _ = _stringLocalizer["b", new { count = 3 }];
                           _ = this.HtmlLocalizer["common:c"];
                           _ = L["d"];
                           _ = dictionary["e"];
                           _ = localizer[name];
                           _ = array[0];
                           """);

        keys.Select(k => (k.Namespace, k.Key, k.HasCount)).ShouldBe([
            ("translation", "a", false),
            ("translation", "b", true),
            ("common", "c", false),
            ("translation", "d", false)
        ]);
    }

    [Fact]
    public void Extract_FileAndDirectoryExists_ShouldBeIgnored()
    {
        Extract("""
                File.Exists("a.json");
                System.IO.File.Exists("b.json");
                Directory.Exists("c");
                i18n.Exists("d");
                """).Select(k => k.Key).ShouldBe(["d"]);
    }

    [Fact]
    public void Extract_CustomFunctionNames_ShouldBeUsed()
    {
        _options.FunctionNames.Add("Translate");
        _options.DefaultNamespace = "app";
        _options.NamespaceSeparator = "::";

        Extract("""
                Translate("a");
                helper.Translate("ns::b");
                i18n.T("c:d");
                """).Select(k => (k.Namespace, k.Key)).ShouldBe([("app", "a"), ("ns", "b"), ("app", "c:d")]);
    }

    [Fact]
    public void Extract_InvalidCode_ShouldStillFindKeys()
    {
        new CSharpKeyExtractor(_options).Extract("class X { void M() { i18n.T(\"a\"); i18n.T(\"b\" ", "Broken.cs").Select(k => k.Key).ShouldBe(["a", "b"]);
    }

    private List<ExtractedKey> Extract(string statements, string members = "", string moreStatements = "")
    {
        var code = $$"""
                     using I18Next.Net;

                     public class Sample(II18Next i18n)
                     {
                         {{members}}

                         public async Task Run()
                         {
                             {{statements.Replace("\n", "\n        ")}}
                             {{moreStatements}}
                         }
                     }
                     """;

        return new CSharpKeyExtractor(_options).Extract(code, "File.cs").ToList();
    }
}
