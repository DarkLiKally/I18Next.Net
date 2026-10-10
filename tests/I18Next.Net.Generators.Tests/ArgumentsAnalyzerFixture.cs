using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class ArgumentsAnalyzerFixture
{
    private static readonly Dictionary<string, string> Files = new()
    {
        ["/app/locales/en/translation.json"] = """
                                               {
                                                   "greeting": "Hello {{name}}, you have {{count}} messages",
                                                   "welcome": "Welcome {{user.name}} to {{place}}",
                                                   "item_one": "{{count}} item of {{owner}}",
                                                   "item_other": "{{count}} items of {{owner}}",
                                                   "plain": "Plain",
                                                   "friend": "A friend",
                                                   "friend_male": "A boyfriend of {{name}}",
                                                   "rank_one": "{{count}} rank",
                                                   "rank_other": "{{count}} ranks",
                                                   "rank_ordinal_one": "{{count}}st rank of {{team}}",
                                                   "rank_ordinal_other": "{{count}}th rank of {{team}}",
                                                   "nested": "$t(greeting) and {{extra}}",
                                                   "formatted": "{{amount, currency(EUR)}} and {{- html}}",
                                                   "pair": "{{name}} and {{names}}",
                                                   "dashed": "{{first-name}}",
                                                   "keyword": "{{class}}",
                                                   "menu": { "title": "Menu of {{owner}}" },
                                                   "list": [ "{{a}}" ]
                                               }
                                               """,
        ["/app/locales/de/translation.json"] = """{ "greeting": "Hallo {{name}} {{title}}" }""",
        ["/app/locales/en/common.json"] = """{ "save": "Save {{what}}" }"""
    };

    [Fact]
    public async Task Analyze_MisspelledArgument_ShouldReportMissingAndUnusedArgument()
    {
        var diagnostics = await AnalyzeAsync("""i18n.T("greeting", new { nam = "Jane", count = 2 });""");

        diagnostics.Length.ShouldBe(2);

        var missing = diagnostics.Single(d => d.Id == "I18N011");
        missing.GetMessage().ShouldBe("The translation 'greeting' uses the placeholder 'name' which is not passed in the arguments");
        missing.Severity.ShouldBe(DiagnosticSeverity.Warning);
        missing.Properties["Placeholder"].ShouldBe("name");
        missing.Properties["Member"].ShouldBe("nam");
        GetText(missing).ShouldBe("""new { nam = "Jane", count = 2 }""");

        var unused = diagnostics.Single(d => d.Id == "I18N012");
        unused.GetMessage().ShouldBe("The argument 'nam' is not used by the translation 'greeting'");
        unused.Severity.ShouldBe(DiagnosticSeverity.Info);
        GetText(unused).ShouldBe("nam = \"Jane\"");
    }

    [Fact]
    public async Task Analyze_ProvidedArguments_ShouldNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
                                             var name = "Jane";
                                             i18n.T("greeting", new { name, count = 1 });
                                             i18n.Ta("greeting", new { name, title = "Dr." });
                                             i18n.T("en", "greeting", new { name, defaultValue = "Hi", defaultValue_one = "Hi", lng = "de", joinArrays = "," });
                                             i18n.T("greeting", new { keyPrefix = "menu", other = 1 });
                                             i18n.T("greeting", new { ns = "common", other = 1 });
                                             i18n.T("welcome", new { user = new { name }, place = "home" });
                                             i18n.T("plain");
                                             i18n.T("plain", new { });
                                             i18n.T("item", new { count = 1, owner = name });
                                             i18n.T("item");
                                             i18n.T("item_one", new { owner = name });
                                             i18n.T("friend", new { context = "female" });
                                             i18n.T("friend", new { context = name });
                                             i18n.T("friend", new { context = (string)null });
                                             i18n.T("rank", new { count = 1 });
                                             i18n.T("rank", new { count = 1, ordinal = true, team = name });
                                             i18n.T("rank", new { count = 1, team = name });
                                             i18n.T("nested", new { extra = 1, unrelated = 2 });
                                             i18n.T("formatted", new { amount = 1, html = "<b>" });
                                             i18n.T("menu.title", new { owner = name });
                                             i18n.T("common:save", new { what = name });
                                             i18n.T("translation", "common", "save", new { what = name });
                                             i18n.T("greeting", new System.Collections.Generic.Dictionary<string, object>());
                                             i18n.T("greeting", new { replace = new { name } });
                                             i18n.T("greeting", new { replace = (object)name });
                                             i18n.T("greeting");
                                             i18n.T("greeting", null);
                                             i18n.T<string[]>("list", new { b = 1 });
                                             i18n.TObject("menu", new { other = 1 });
                                             i18n.T(new[] { "greeting" }, new { other = 1 });
                                             i18n.T("unknown", new { other = 1 });
                                             i18n.T("unknownNamespace:greeting", new { other = 1 });
                                             other.T("greeting", new { other = 1 });
                                             """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Analyze_ContextPluralsAndOrdinals_ShouldUseTheSelectedVariants()
    {
        var diagnostics = await AnalyzeAsync("""
                                             i18n.T("friend", new { context = "male" });
                                             i18n.T("item", new { count = 3 });
                                             i18n.T("rank", new { count = 1, ordinal = true });
                                             i18n.T("item", new { count = 1, ordinal = true });
                                             """);

        diagnostics.Select(d => d.GetMessage()).ShouldBe([
            "The translation 'friend' uses the placeholder 'name' which is not passed in the arguments",
            "The translation 'item' uses the placeholder 'owner' which is not passed in the arguments",
            "The translation 'rank' uses the placeholder 'team' which is not passed in the arguments",
            "The translation 'item' uses the placeholder 'owner' which is not passed in the arguments"
        ]);
        diagnostics.ShouldAllBe(d => !d.Properties.ContainsKey("Member"));
    }

    [Fact]
    public async Task Analyze_ReplaceArguments_ShouldBeChecked()
    {
        var diagnostics = await AnalyzeAsync("""i18n.T("greeting", new { count = 1, replace = new { nme = "Jane" } });""");

        var missing = diagnostics.Single(d => d.Id == "I18N011");
        missing.Properties["Member"].ShouldBe("nme");
        GetText(missing).ShouldBe("""new { nme = "Jane" }""");
        diagnostics.Single(d => d.Id == "I18N012").GetMessage().ShouldBe("The argument 'nme' is not used by the translation 'greeting'");
    }

    [Fact]
    public async Task Analyze_AmbiguousOrInvalidCandidates_ShouldNotSuggestMember()
    {
        var diagnostics = await AnalyzeAsync("""
                                             i18n.T("pair", new { nam = 1 });
                                             i18n.T("greeting", new { nam = 1, nme = 2 });
                                             i18n.T("dashed", new { firstName = 1 });
                                             i18n.T("welcome", new { usr = 1, location = 2 });
                                             """);

        var missing = diagnostics.Where(d => d.Id == "I18N011").ToList();

        missing.Select(d => d.Properties["Placeholder"]).ShouldBe(["name", "names", "name", "first-name", "user", "place"]);
        missing.Select(d => d.Properties.GetValueOrDefault("Member")).ShouldBe([null, null, null, null, "usr", null]);
    }

    [Fact]
    public async Task Analyze_NamespacePrefix_ShouldBeInMessage()
    {
        var diagnostics = await AnalyzeAsync("""i18n.T("common:save", new { });""");

        diagnostics.Single().GetMessage().ShouldBe("The translation 'common:save' uses the placeholder 'what' which is not passed in the arguments");
    }

    [Fact]
    public async Task Analyze_StringLocalizer_ShouldCheckArguments()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               using Microsoft.Extensions.Localization;

                                                               namespace Microsoft.Extensions.Localization
                                                               {
                                                                   public interface IStringLocalizer
                                                                   {
                                                                       string this[string name] { get; }

                                                                       string this[string name, params object[] arguments] { get; }
                                                                   }

                                                                   public interface IStringLocalizer<T> : IStringLocalizer
                                                                   {
                                                                   }

                                                                   public static class StringLocalizerExtensions
                                                                   {
                                                                       public static string GetString(this IStringLocalizer localizer, string name, params object[] arguments) => localizer[name, arguments];
                                                                   }
                                                               }

                                                               [I18Next.Net.I18NextResources("locales")]
                                                               public static partial class Texts;

                                                               public class Other
                                                               {
                                                                   public string this[string name, params object[] arguments] => name;
                                                               }

                                                               public static class Usage
                                                               {
                                                                   public static void Use(Microsoft.Extensions.Localization.IStringLocalizer localizer,
                                                                       Microsoft.Extensions.Localization.IStringLocalizer<Usage> typed, Other other)
                                                                   {
                                                                       _ = localizer["greeting", new { nam = 1 }];
                                                                       _ = typed["greeting", new { name = 1 }];
                                                                       _ = localizer.GetString("common:save", new { });
                                                                       _ = localizer["greeting"];
                                                                       _ = localizer["greeting", 1, 2];
                                                                       _ = localizer["greeting", new object[] { new { name = 1 } }];
                                                                       _ = other["greeting", new { nam = 1 }];
                                                                   }
                                                               }
                                                               """, Files, new TranslationArgumentsAnalyzer());

        diagnostics.Where(d => d.Id == "I18N011").Select(d => d.GetMessage()).ShouldBe([
            "The translation 'greeting' uses the placeholder 'name' which is not passed in the arguments",
            "The translation 'common:save' uses the placeholder 'what' which is not passed in the arguments"
        ]);
        diagnostics.Count(d => d.Id == "I18N012").ShouldBe(1);
    }

    [Fact]
    public async Task Analyze_WithoutResources_ShouldNotReport()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               public static class Usage
                                                               {
                                                                   public static void Use(I18Next.Net.II18Next i18n) => i18n.T("greeting", new { nam = 1 });
                                                               }
                                                               """, Files, new TranslationArgumentsAnalyzer());

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Analyze_LegacyJsonFormat_ShouldUseSingularAndPluralKeys()
    {
        var diagnostics = await GeneratorTestHost.AnalyzeAsync("""
                                                               [I18Next.Net.I18NextResources("locales", JsonFormatVersion = 3)]
                                                               public static partial class Texts;

                                                               public static class Usage
                                                               {
                                                                   public static void Use(I18Next.Net.II18Next i18n) => i18n.T("item", new { count = 1 });
                                                               }
                                                               """, new Dictionary<string, string>
        {
            ["/app/locales/en/translation.json"] = """{ "item": "{{count}} item of {{owner}}", "item_plural": "{{count}} items of {{owners}}" }"""
        }, new TranslationArgumentsAnalyzer());

        diagnostics.Select(d => d.Properties["Placeholder"]).ShouldBe(["owners", "owner"]);
    }

    private static string GetText(Diagnostic diagnostic)
    {
        return diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string statements)
    {
        return GeneratorTestHost.AnalyzeAsync($$"""
                                                [I18Next.Net.I18NextResources("locales")]
                                                public static partial class Texts;

                                                public interface IOther
                                                {
                                                    string T(string key, object args);
                                                }

                                                public static class Usage
                                                {
                                                    public static void Use(I18Next.Net.II18Next i18n, IOther other)
                                                    {
                                                {{statements}}
                                                    }
                                                }
                                                """, Files, new TranslationArgumentsAnalyzer());
    }
}
