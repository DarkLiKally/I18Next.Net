using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class GeneratorFixture
{
    private const string Source = """
                                  namespace App;

                                  [I18Next.Net.I18NextResources("locales")]
                                  public static partial class Texts;
                                  """;

    [Fact]
    public void Generate_ValidFiles_ShouldCompileWithoutDiagnostics()
    {
        var result = GeneratorTestHost.Run(Source + """

                                                    public static class Usage
                                                    {
                                                        public static string Use(I18Next.Net.II18Next i18n) => i18n.Translation().Greeting("Jane") + Texts.Keys.Translation.Greeting;
                                                    }
                                                    """, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """{ "greeting": "Hello {{name}}" }""",
            ["/app/locales/de/translation.json"] = """{ "greeting": "Hallo {{name}}" }"""
        });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("public string Greeting(object name, string language = null)");
        result.Source.ShouldContain("/// <summary><c>greeting</c>: Hello {{name}}</summary>");
    }

    [Fact]
    public void Generate_InvalidJson_ShouldReportErrorWithPosition()
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = "{\n  \"a\": \"b\"\n  \"c\": \"d\"\n}",
            ["/app/locales/en/common.json"] = """{ "ok": "fine" }"""
        });

        var diagnostic = result.Diagnostics.Single(d => d.Id == "I18N001");

        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage().ShouldContain("Expected ',' or '}'");
        diagnostic.Location.GetLineSpan().StartLinePosition.Line.ShouldBe(2);
        result.Source.ShouldContain("CommonNamespace");
    }

    [Theory]
    [InlineData("[]", "Expected '{' at the start of the file")]
    [InlineData("{ \"a\": \"b\" } x", "Unexpected content after the end of the root object")]
    [InlineData("{ a: \"b\" }", "Expected a property name")]
    [InlineData("{ \"a\" \"b\" }", "Expected ':' after the property name")]
    [InlineData("{ \"a\": \"b", "Unterminated string")]
    [InlineData("{ \"a\": \"\\u12\" }", "Invalid unicode escape sequence")]
    [InlineData("{ \"a\": tru }", "Unexpected character")]
    [InlineData("{ \"a\": [ \"b\" \"c\" ] }", "Expected ',' or ']'")]
    [InlineData("{ \"a\": } ", "Unexpected character")]
    public void Generate_MalformedJson_ShouldReportError(string json, string message)
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string> { ["/app/locales/en/translation.json"] = json });

        result.Diagnostics.ShouldContain(d => d.Id == "I18N001" && d.GetMessage().Contains(message));
    }

    [Fact]
    public void Generate_JsonFeatures_ShouldBeParsed()
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """
                                                   // comment
                                                   {
                                                       /* block */
                                                       "escaped": "Line\nTab\t\"quoted\" \u00e4 \\ \/ \b \f \r",
                                                       "number": 1.5e3,
                                                       "flag": true,
                                                       "off": false,
                                                       "nothing": null,
                                                       "list": [ "a", "b", ],
                                                       "empty": [],
                                                       "objects": [ { "label": "x" } ],
                                                   }
                                                   """
        });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("public const string Number = \"translation:number\";");
        result.Source.ShouldContain("public const string Flag = \"translation:flag\";");
        result.Source.ShouldContain("public const string Off = \"translation:off\";");
        result.Source.ShouldNotContain("Nothing");
        result.Source.ShouldContain("public string[] List(string language = null)");
        result.Source.ShouldContain("public ObjectsGroup Objects =>");
        result.Source.ShouldContain("ä");
    }

    [Fact]
    public void Generate_MissingKeysAndPlaceholders_ShouldReportWarnings()
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """{ "a": "A {{name}}", "b": "B", "item_one": "{{count}} item", "item_other": "{{count}} items" }""",
            ["/app/locales/en/common.json"] = """{ "save": "Save" }""",
            ["/app/locales/de/translation.json"] = """{ "a": "A {{nme}} {{name}}", "item_one": "{{count}} Element", "item_other": "{{count}} Elemente" }""",
            ["/app/locales/ar/translation.json"] = """{ "a": "A", "b": "B", "item_zero": "0", "item_many": "{{count}}" }""",
            ["/app/locales/ar/common.json"] = """{ "save": "S" }"""
        });

        var missingKey = result.Diagnostics.Single(d => d.Id == "I18N002");
        missingKey.GetMessage().ShouldBe("The key 'b' of the namespace 'translation' is missing in the language 'de'");
        missingKey.Location.GetLineSpan().Path.ShouldBe("/app/locales/de/translation.json");

        result.Diagnostics.Single(d => d.Id == "I18N003").GetMessage()
            .ShouldBe("The key 'a' of the namespace 'translation' uses the placeholders 'nme' in the language 'de' which the source language does not use");

        result.Diagnostics.Single(d => d.Id == "I18N004").GetMessage().ShouldBe("The namespace 'common' is missing in the language 'de'");
        result.CompilationErrors.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_NoSourceFiles_ShouldReportWarning()
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string>
        {
            ["/app/other/en/translation.json"] = """{ "a": "A" }""",
            ["/app/locales/de/translation.json"] = """{ "a": "A" }""",
            ["/app/locales/readme.txt"] = "text"
        });

        result.Diagnostics.Single().Id.ShouldBe("I18N005");
        result.Diagnostics.Single().Location.GetLineSpan().Path.ShouldBe("Test.cs");
        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_AttributeOptions_ShouldBeUsed()
    {
        var result = GeneratorTestHost.Run("""
                                           namespace App;

                                           [I18Next.Net.I18NextResources("./res/i18n/", SourceLanguage = "de", NamespaceSeparator = "::", JsonFormatVersion = 3, DefaultNamespace = "app")]
                                           public partial class Texts;
                                           """, new Dictionary<string, string>
        {
            ["C:\\project\\res\\i18n\\de\\app.json"] = """{ "item": "{{count}} Element", "item_plural": "{{count}} Elemente", "other_1": "x" }"""
        });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("public const string Item = \"app::item\";");
        result.Source.ShouldContain("public string Item(int? count = null, string language = null)");
        result.Source.ShouldContain("public string Other(int count, string language = null)");
        result.Source.ShouldContain("public static AppNamespace App(global::I18Next.Net.II18Next i18n)");
    }

    [Fact]
    public void Generate_NestedGenericAndGlobalTypes_ShouldCompile()
    {
        var files = new Dictionary<string, string> { ["/locales/en/translation.json"] = """{ "a": "A" }""" };

        var nested = GeneratorTestHost.Run("""
                                           namespace App;

                                           public partial class Outer<T>
                                           {
                                               internal partial struct Inner
                                               {
                                                   [I18Next.Net.I18NextResources("locales")]
                                                   private static partial class Texts;
                                               }
                                           }
                                           """, files);

        nested.Diagnostics.ShouldBeEmpty();
        nested.CompilationErrors.ShouldBeEmpty();
        nested.Source.ShouldContain("partial class Outer<T>");
        nested.Source.ShouldContain("public static TranslationNamespace Translation(global::I18Next.Net.II18Next i18n)");

        var global = GeneratorTestHost.Run("""
                                           [I18Next.Net.I18NextResources("locales")]
                                           public static partial class Texts;

                                           public partial record Model
                                           {
                                               [I18Next.Net.I18NextResources("locales")]
                                               public partial record Inner;
                                           }
                                           """, files);

        global.CompilationErrors.ShouldBeEmpty();
        global.Sources.Length.ShouldBe(2);
        global.Sources[0].ShouldNotContain("\nnamespace ");
    }

    [Fact]
    public void Generate_NameCollisions_ShouldProduceValidIdentifiers()
    {
        var result = GeneratorTestHost.Run(Source, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """
                                                   {
                                                       "texts": "Same as the class",
                                                       "keys": "Keys",
                                                       "a-b": "Dash",
                                                       "a_b": "Underscore",
                                                       "aB": "Camel",
                                                       "aBAsync": "Async collision",
                                                       "!!!": "No letters",
                                                       "params": "{{class}} {{language}} {{args}} {{first-name}} {{firstName}} {{count}} {{context}}",
                                                       "toObject": "Reserved",
                                                       "group": { "group": "Same as the group", "key": "Key", "inner": { "x": "y" }, "innerGroup": "z" }
                                                   }
                                                   """,
            ["/app/locales/en/translation-v2.json"] = """{ "a": "b" }"""
        });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("public string Texts(");
        result.Source.ShouldContain("ABAsync2(");
        result.Source.ShouldContain("AB(");
        result.Source.ShouldContain("AB2(");
        result.Source.ShouldContain("AB3(");
        result.Source.ShouldContain("public string Key(");
        result.Source.ShouldContain("object @class, object language2, object args2, object firstName, object firstName2, object count2, object context2");
        result.Source.ShouldContain("ToObject2(");
        result.Source.ShouldContain("TranslationV2Namespace");
    }

    [Fact]
    public void Generate_PluralsContextsAndOrdinals_ShouldCreateParameters()
    {
        var result = GeneratorTestHost.Run("""
                                           namespace App;

                                           public partial record struct Model
                                           {
                                               [I18Next.Net.I18NextResources("locales")]
                                               internal static partial class Texts;
                                           }
                                           """, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """
                                                   {
                                                       "item_one": "{{count}} item",
                                                       "item_other": "{{count}} items",
                                                       "rank_ordinal_one": "{{count}}st",
                                                       "rank_ordinal_other": "{{count}}th",
                                                       "both_one": "{{count}} thing",
                                                       "both_other": "{{count}} things",
                                                       "both_ordinal_other": "{{count}}th thing",
                                                       "friend": "A friend",
                                                       "friend_male": "A boyfriend of {{name}}",
                                                       "friend_male_other": "{{count}} boyfriends",
                                                       "list": [ "{{a}}", "{{b}} {{a}}" ],
                                                       "xAsync": "Async first",
                                                       "x": "Then x"
                                                   }
                                                   """
        });

        result.Diagnostics.ShouldBeEmpty();
        result.CompilationErrors.ShouldBeEmpty();
        result.Source.ShouldContain("partial record struct Model");
        result.Source.ShouldContain("public string Item(int count, string language = null)");
        result.Source.ShouldContain("/// <summary><c>item</c>: {{count}} item (plural)</summary>");
        result.Source.ShouldContain("public string Rank(int count, string language = null)");
        result.Source.ShouldContain("args[\"ordinal\"] = true;");
        result.Source.ShouldContain("public string Both(int count, bool ordinal = false, string language = null)");
        result.Source.ShouldContain("(plural; ordinal)");
        result.Source.ShouldContain("public string Friend(object name, int? count = null, string context = null, string language = null)");
        result.Source.ShouldContain("(plural; contexts: male)");
        result.Source.ShouldContain("public string[] List(object a, object b, string language = null)");
        result.Source.ShouldContain("public string XAsync(");
        result.Source.ShouldContain("public string X_(");
    }

    [Fact]
    public void Generate_SameInputTwice_ShouldUseCachedOutput()
    {
        var files = new Dictionary<string, string> { ["/app/locales/en/translation.json"] = """{ "a": "A" }""" };
        var compilation = GeneratorTestHost.CreateCompilation(Source);
        var driver = CSharpGeneratorDriver.Create(new[] { new I18NextResourcesGenerator().AsSourceGenerator() }, GeneratorTestHost.CreateAdditionalTexts(files),
            new CSharpParseOptions(LanguageVersion.Latest), driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(s => s.Value).SelectMany(s => s.Outputs);

        outputs.ShouldAllBe(o => o.Reason == IncrementalStepRunReason.Cached || o.Reason == IncrementalStepRunReason.Unchanged);
    }
}
