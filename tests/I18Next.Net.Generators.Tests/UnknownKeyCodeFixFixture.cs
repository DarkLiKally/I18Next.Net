using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Generators.CodeFixes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class UnknownKeyCodeFixFixture
{
    private const string EnglishPath = "/app/locales/en/translation.json";
    private const string GermanPath = "/app/locales/de/translation.json";

    private static readonly Dictionary<string, string> Files = new()
    {
        [EnglishPath] = "{\n  \"welcome\": \"Hello {{name}}\",\n  \"menu\": {\n    \"title\": \"Menu\"\n  }\n}",
        [GermanPath] = "{\n    \"menu\": {\n        \"title\": \"Menü\"\n    },\n    \"welcome\": \"Hallo {{name}}\"\n}",
        ["/app/locales/en/common.json"] = "{ \"save\": \"Save\" }"
    };

    [Fact]
    public async Task Fix_Suggestion_ShouldReplaceTheKey()
    {
        var (project, actions) = await GetActionsAsync("""i18n.T("welcom", new { name = 1 });""");

        actions.Select(a => a.Title).ShouldBe([
            "Change to 'welcome'",
            "Add 'welcom' to the 'en' translation file",
            "Add 'welcom' to all translation files"
        ]);

        var solution = await CodeFixTestHost.ApplyAsync(actions[0]);

        (await CodeFixTestHost.GetTextAsync(solution, CodeFixTestHost.SourcePath)).ShouldContain("""i18n.T("welcome", new { name = 1 });""");
        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath)).ShouldBe(Files[EnglishPath]);
        solution.Projects.Single().Id.ShouldBe(project.Id);
    }

    [Fact]
    public async Task Fix_SuggestionForVerbatimLiteralWithNamespace_ShouldReplaceTheKey()
    {
        var (_, actions) = await GetActionsAsync("""i18n.T(@"common:sav" /* key */);""");

        var solution = await CodeFixTestHost.ApplyAsync(actions.Single(a => a.Title == "Change to 'common:save'"));

        (await CodeFixTestHost.GetTextAsync(solution, CodeFixTestHost.SourcePath)).ShouldContain("""i18n.T("common:save" /* key */);""");
    }

    [Fact]
    public async Task Fix_ConstantKey_ShouldOnlyAddTheKey()
    {
        var (_, actions) = await GetActionsAsync("""
                                                 const string Key = "welcom";
                                                 i18n.T(Key);
                                                 """);

        actions.Select(a => a.Title).ShouldBe(["Add 'welcom' to the 'en' translation file", "Add 'welcom' to all translation files"]);
    }

    [Fact]
    public async Task Fix_AddToSourceFile_ShouldOnlyChangeTheSourceFile()
    {
        var (_, actions) = await GetActionsAsync("""i18n.T("menu.subtitle");""");

        var solution = await CodeFixTestHost.ApplyAsync(actions.Single(a => a.Title == "Add 'menu.subtitle' to the 'en' translation file"));

        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath))
            .ShouldBe("{\n  \"welcome\": \"Hello {{name}}\",\n  \"menu\": {\n    \"title\": \"Menu\",\n    \"subtitle\": \"subtitle\"\n  }\n}");
        (await CodeFixTestHost.GetTextAsync(solution, GermanPath)).ShouldBe(Files[GermanPath]);
    }

    [Fact]
    public async Task Fix_AddToAllFiles_ShouldFollowTheOrderOfTheSourceFile()
    {
        var (_, actions) = await GetActionsAsync("""i18n.T("menu.subtitle");""");

        var solution = await CodeFixTestHost.ApplyAsync(actions.Single(a => a.Title == "Add 'menu.subtitle' to all translation files"));

        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath))
            .ShouldBe("{\n  \"welcome\": \"Hello {{name}}\",\n  \"menu\": {\n    \"title\": \"Menu\",\n    \"subtitle\": \"subtitle\"\n  }\n}");
        (await CodeFixTestHost.GetTextAsync(solution, GermanPath))
            .ShouldBe("{\n    \"menu\": {\n        \"title\": \"Menü\",\n        \"subtitle\": \"subtitle\"\n    },\n    \"welcome\": \"Hallo {{name}}\"\n}");
    }

    [Fact]
    public async Task Fix_KeyBelowAValue_ShouldNotAddTheKey()
    {
        var (_, actions) = await GetActionsAsync("""i18n.T("welcome.title");""");

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_SourceFileNotInProject_ShouldNotAddTheKey()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("missing");"""), Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single();
        var withoutFiles = project.AdditionalDocuments.Aggregate(project, (p, d) => p.RemoveAdditionalDocument(d.Id));

        var actions = await CodeFixTestHost.GetActionsAsync(new UnknownKeyCodeFixProvider(), withoutFiles, [diagnostic]);

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_EmptyKeySegment_ShouldNotAddTheKey()
    {
        var (_, actions) = await GetActionsAsync("""i18n.T("menu..title");""");

        actions.Select(a => a.Title).ShouldBe(["Change to 'menu.title'"]);
    }

    [Fact]
    public async Task Fix_DiagnosticWithoutProperties_ShouldNotOfferFixes()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("welcom");"""), Files);
        var analyzed = (await CodeFixTestHost.AnalyzeAsync(project)).Single();
        var diagnostic = Diagnostic.Create(analyzed.Descriptor, analyzed.Location);

        var actions = await CodeFixTestHost.GetActionsAsync(new UnknownKeyCodeFixProvider(), project, [diagnostic]);

        actions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{ \"a\": \"b\" } x")]
    [InlineData("{ a: \"b\" }")]
    [InlineData("{ \"a\" \"b\" }")]
    [InlineData("{ \"a\": }")]
    [InlineData("{ \"a\": \"b\" \"c\": \"d\" }")]
    [InlineData("{ \"a\": \"b")]
    [InlineData("{ \"a\": \"b\\")]
    [InlineData("{ \"a\": \"\\u12\" }")]
    [InlineData("{ \"a\": [ \"b\" \"c\" ] }")]
    [InlineData("{ \"a\": [ \"b\", [ 1, { } ] ] /* unterminated")]
    public async Task Fix_TranslationFileChangedToInvalidJson_ShouldNotAddTheKey(string json)
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("missing");"""), Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single();
        var english = project.AdditionalDocuments.Single(d => d.FilePath == EnglishPath);
        var changed = project.Solution.WithAdditionalDocumentText(english.Id, Microsoft.CodeAnalysis.Text.SourceText.From(json)).GetProject(project.Id);

        var actions = await CodeFixTestHost.GetActionsAsync(new UnknownKeyCodeFixProvider(), changed, [diagnostic]);

        actions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("{\n    \"a\": \"A\"\n}", "b", "{\n    \"a\": \"A\",\n    \"b\": \"b\"\n}")]
    [InlineData("{\n  \"e\": \"\\n\\r\\t\\b\\f\\/\\\\\\\"\\u00e4\",\n  \"l\": [ [ 1 ], { \"x\": [] } ]\n}", "f\\\n\r\b\f",
        "{\n  \"e\": \"\\n\\r\\t\\b\\f\\/\\\\\\\"\\u00e4\",\n  \"f\\\\\\n\\r\\b\\f\": \"f\\\\\\n\\r\\b\\f\",\n  \"l\": [ [ 1 ], { \"x\": [] } ]\n}")]
    [InlineData("{\n  \"a\": \"A\",\n  \"c\": \"C\"\n}", "b", "{\n  \"a\": \"A\",\n  \"b\": \"b\",\n  \"c\": \"C\"\n}")]
    [InlineData("{\n  \"c\": \"C\",\n  \"a\": \"A\"\n}", "b", "{\n  \"c\": \"C\",\n  \"a\": \"A\",\n  \"b\": \"b\"\n}")]
    [InlineData("{\n  \"b\": \"B\",\n  \"c\": \"C\"\n}", "a", "{\n  \"a\": \"a\",\n  \"b\": \"B\",\n  \"c\": \"C\"\n}")]
    [InlineData("{\n  \"a\": \"A\", \"c\": \"C\"\n}", "b", "{\n  \"a\": \"A\", \"b\": \"b\", \"c\": \"C\"\n}")]
    [InlineData("{\n  \"a\": \"A\"\n}", "menu.title", "{\n  \"a\": \"A\",\n  \"menu\": {\n    \"title\": \"title\"\n  }\n}")]
    [InlineData("{\n  \"menu\": {\n    \"a\": \"A\"\n  }\n}", "menu.b", "{\n  \"menu\": {\n    \"a\": \"A\",\n    \"b\": \"b\"\n  }\n}")]
    [InlineData("{\n  \"menu\": {}\n}", "menu.a", "{\n  \"menu\": {\n    \"a\": \"a\"\n  }\n}")]
    [InlineData("{ \"a\": \"A\" }", "b", "{ \"a\": \"A\", \"b\": \"b\" }")]
    [InlineData("{ \"a\": \"A\" }", "x.y", "{ \"a\": \"A\", \"x\": { \"y\": \"y\" } }")]
    [InlineData("{\"a\":\"A\"}", "b", "{\"a\":\"A\", \"b\":\"b\"}")]
    [InlineData("{\n  \"a\": \"A\" }", "b", "{\n  \"a\": \"A\", \"b\": \"b\" }")]
    [InlineData("{}", "a", "{\n  \"a\": \"a\"\n}")]
    [InlineData("{\n  // nothing yet\n}", "a", "{\n  \"a\": \"a\"\n  // nothing yet\n}")]
    [InlineData("{\n  \"a\": \"A\",\n}", "b", "{\n  \"a\": \"A\",\n  \"b\": \"b\",\n}")]
    [InlineData("{ \"a\": \"A\", }", "b", "{ \"a\": \"A\", \"b\": \"b\", }")]
    [InlineData("{\n  \"a\": \"A\" // note\n}", "b", "{\n  \"a\": \"A\", // note\n  \"b\": \"b\"\n}")]
    [InlineData("{\n  \"a\": \"A\" /* note */\n}", "b", "{\n  \"a\": \"A\", \"b\": \"b\" /* note */\n}")]
    [InlineData("{\r\n\t\"a\": \"A\"\r\n}", "b", "{\r\n\t\"a\": \"A\",\r\n\t\"b\": \"b\"\r\n}")]
    [InlineData("{\n  \"menu.title\": \"T\"\n}", "menu.sub", "{\n  \"menu.title\": \"T\",\n  \"menu.sub\": \"sub\"\n}")]
    [InlineData("{\n  \"a\": [ 1, { \"x\": true } ],\n  \"n\": null\n}", "say \"hi\"\t\u0001", "{\n  \"a\": [ 1, { \"x\": true } ],\n  \"n\": null,\n  \"say \\\"hi\\\"\\t\\u0001\": \"say \\\"hi\\\"\\t\\u0001\"\n}")]
    [InlineData("{\n  \"\\u0062\": { \"c\": -1.5e3 }\n}", "b.d", "{\n  \"\\u0062\": { \"c\": -1.5e3, \"d\": \"d\" }\n}")]
    public async Task Fix_AddToSourceFile_ShouldKeepTheFormatting(string json, string key, string expected)
    {
        var files = new Dictionary<string, string> { [EnglishPath] = json };
        var (_, actions) = await GetActionsAsync($"i18n.T({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(key, true)});", files);

        var solution = await CodeFixTestHost.ApplyAsync(actions.Single(a => a.Title.StartsWith("Add")));

        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(FixAllScope.Document)]
    [InlineData(FixAllScope.Project)]
    [InlineData(FixAllScope.Solution)]
    public async Task FixAll_AddToAllFiles_ShouldAddEveryKey(FixAllScope scope)
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""
                                                         i18n.T("zeta");
                                                         i18n.T("menu.subtitle");
                                                         i18n.T("zeta");
                                                         i18n.T("common:cancel");
                                                         """), Files);
        var diagnostics = await CodeFixTestHost.AnalyzeAsync(project);
        var action = await GetFixAllAsync(project, scope, "I18N010.AddToAllFiles", diagnostics);

        action!.Title.ShouldBe("Add the keys to all translation files");

        var solution = await CodeFixTestHost.ApplyAsync(action);

        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath))
            .ShouldBe("{\n  \"welcome\": \"Hello {{name}}\",\n  \"menu\": {\n    \"title\": \"Menu\",\n    \"subtitle\": \"subtitle\"\n  },\n  \"zeta\": \"zeta\"\n}");
        (await CodeFixTestHost.GetTextAsync(solution, GermanPath))
            .ShouldBe("{\n    \"menu\": {\n        \"title\": \"Menü\",\n        \"subtitle\": \"subtitle\"\n    },\n    \"zeta\": \"zeta\",\n    \"welcome\": \"Hallo {{name}}\"\n}");
        (await CodeFixTestHost.GetTextAsync(solution, "/app/locales/en/common.json")).ShouldBe("{ \"save\": \"Save\", \"cancel\": \"cancel\" }");
    }

    [Fact]
    public async Task FixAll_AddToSourceFile_ShouldOnlyChangeTheSourceFiles()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("zeta");"""), Files);
        var diagnostics = await CodeFixTestHost.AnalyzeAsync(project);
        var action = await GetFixAllAsync(project, FixAllScope.Document, "I18N010.AddToSourceFile", diagnostics);

        action!.Title.ShouldBe("Add the keys to the translation files of the source language");

        var solution = await CodeFixTestHost.ApplyAsync(action);

        (await CodeFixTestHost.GetTextAsync(solution, EnglishPath)).ShouldEndWith(",\n  \"zeta\": \"zeta\"\n}");
        (await CodeFixTestHost.GetTextAsync(solution, GermanPath)).ShouldBe(Files[GermanPath]);
    }

    [Fact]
    public async Task FixAll_OtherActionsOrNoDiagnostics_ShouldReturnNull()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("welcom");"""), Files);
        var diagnostics = await CodeFixTestHost.AnalyzeAsync(project);

        (await GetFixAllAsync(project, FixAllScope.Document, "I18N010.Change.welcome", diagnostics)).ShouldBeNull();
        (await GetFixAllAsync(project, FixAllScope.Document, "I18N010.AddToSourceFile", [])).ShouldBeNull();
    }

    private static Task<CodeAction> GetFixAllAsync(Project project, FixAllScope scope, string equivalenceKey, ImmutableArray<Diagnostic> diagnostics)
    {
        var provider = new UnknownKeyCodeFixProvider();
        var context = new FixAllContext(project.Documents.Single(), provider, scope, equivalenceKey, ["I18N010"],
            new CodeFixTestHost.DiagnosticProvider(diagnostics), CancellationToken.None);

        return provider.GetFixAllProvider().GetFixAsync(context);
    }

    private static async Task<(Project Project, List<CodeAction> Actions)> GetActionsAsync(string statements, Dictionary<string, string> files = null)
    {
        var project = CodeFixTestHost.CreateProject(Wrap(statements), files ?? Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single(d => d.Id == "I18N010");

        return (project, await CodeFixTestHost.GetActionsAsync(new UnknownKeyCodeFixProvider(), project, [diagnostic]));
    }

    private static string Wrap(string statements)
    {
        return $$"""
                 [I18Next.Net.I18NextResources("locales")]
                 public static partial class Texts;

                 public static class Usage
                 {
                     public static void Use(I18Next.Net.II18Next i18n)
                     {
                 {{statements}}
                     }
                 }
                 """;
    }
}
