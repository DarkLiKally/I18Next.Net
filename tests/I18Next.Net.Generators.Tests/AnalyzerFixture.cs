using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class AnalyzerFixture
{
    private static readonly Dictionary<string, string> Files = new()
    {
        ["/app/locales/en/translation.json"] =
            """{ "welcome": "Hi", "item_one": "1", "item_other": "n", "friend": "f", "friend_male": "m", "menu": { "title": "t" }, "list": [ "a" ] }""",
        ["/app/locales/en/common.json"] = """{ "save": "Save" }""",
        ["/app/locales/de/translation.json"] = """{ "onlyGerman": "x" }""",
        ["/app/locales/en/broken.json"] = "{"
    };

    [Fact]
    public async Task Analyze_UnknownKeys_ShouldReportWarnings()
    {
        var diagnostics = await AnalyzeAsync("""
                                             i18n.T("welcome");
                                             i18n.T("missing");
                                             i18n.Ta("de", "onlyGerman");
                                             i18n.TObject("common:unknown");
                                             i18n.Exists(key: "menu.missing");
                                             i18n.T("de", "common", "nope");
                                             """);

        diagnostics.Select(d => d.GetMessage()).ShouldBe([
            "The translation key 'missing' does not exist in the namespace 'translation'",
            "The translation key 'onlyGerman' does not exist in the namespace 'translation'",
            "The translation key 'unknown' does not exist in the namespace 'common'",
            "The translation key 'menu.missing' does not exist in the namespace 'translation'",
            "The translation key 'nope' does not exist in the namespace 'common'"
        ]);
        diagnostics.ShouldAllBe(d => d.Id == "I18N010");
    }

    [Fact]
    public async Task Analyze_ResolvableKeys_ShouldNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
                                             i18n.T("welcome");
                                             i18n.T("item", new { count = 2 });
                                             i18n.T("item_one");
                                             i18n.T("friend_male");
                                             i18n.T("friend");
                                             i18n.TObject("menu");
                                             i18n.T("menu.title");
                                             i18n.T<string[]>("list");
                                             i18n.T("list.0");
                                             i18n.T("common:save");
                                             i18n.T("unknownNamespace:anything");
                                             i18n.T("de", "broken", "key");
                                             i18n.T(new[] { "missing", "welcome" });
                                             var key = "dynamic";
                                             i18n.T(key);
                                             i18n.Dir("missing");
                                             concrete.T("welcome");
                                             concrete.T("concreteMissing");
                                             other.T("missing");
                                             """);

        diagnostics.Single().GetMessage().ShouldBe("The translation key 'concreteMissing' does not exist in the namespace 'translation'");
    }

    [Fact]
    public async Task Analyze_WithoutResourcesAttribute_ShouldNotReport()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               public static class Usage
                                                               {
                                                                   public static void Use(I18Next.Net.II18Next i18n) => i18n.T("missing");
                                                               }
                                                               """, Files);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Analyze_WithoutFiles_ShouldNotReport()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               [I18Next.Net.I18NextResources("locales")]
                                                               public static partial class Texts;

                                                               public static class Usage
                                                               {
                                                                   public static void Use(I18Next.Net.II18Next i18n) => i18n.T("missing");
                                                               }
                                                               """, new Dictionary<string, string>());

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Analyze_CustomOptions_ShouldBeUsed()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               namespace App.Resources
                                                               {
                                                                   [I18Next.Net.I18NextResources("locales", DefaultNamespace = "common", NamespaceSeparator = "::")]
                                                                   public static partial class Texts;
                                                               }

                                                               public static class Usage
                                                               {
                                                                   public static void Use(I18Next.Net.II18Next i18n)
                                                                   {
                                                                       i18n.T("save");
                                                                       i18n.T("translation::welcome");
                                                                       i18n.T("translation:welcome");
                                                                   }
                                                               }
                                                               """, Files);

        diagnostics.Single().GetMessage().ShouldBe("The translation key 'translation:welcome' does not exist in the namespace 'common'");
    }

    private static Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>> AnalyzeAsync(string statements)
    {
        return GeneratorTestHost.AnalyzeAsync($$"""
                                                [I18Next.Net.I18NextResources("locales")]
                                                public static partial class Texts;

                                                public interface IOther
                                                {
                                                    string T(string key);
                                                }

                                                public static class Usage
                                                {
                                                    public static void Use(I18Next.Net.II18Next i18n, I18Next.Net.I18NextNet concrete, IOther other)
                                                    {
                                                {{statements}}
                                                    }
                                                }
                                                """, Files);
    }
}
