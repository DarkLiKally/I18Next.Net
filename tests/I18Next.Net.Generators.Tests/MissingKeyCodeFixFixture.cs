using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Generators.CodeFixes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class MissingKeyCodeFixFixture
{
    private const string Source = """
                                  [I18Next.Net.I18NextResources("locales")]
                                  public static partial class Texts;
                                  """;

    private const string EnglishPath = "/app/locales/en/translation.json";
    private const string GermanPath = "/app/locales/de/translation.json";

    private static readonly Dictionary<string, string> Files = new()
    {
        [EnglishPath] = "{\n  \"a\": \"A {{x}}\",\n  \"b\": \"B\",\n  \"item_one\": \"{{count}} item\",\n  \"item_other\": \"{{count}} items\",\n  \"list\": [ \"x\" ],\n" +
                        "  \"menu\": {\n    \"title\": \"T\"\n  }\n}",
        [GermanPath] = "{\n  \"b\": \"Bé\"\n}"
    };

    [Fact]
    public async Task Generate_MissingKey_ShouldProvideTheSourceEntries()
    {
        var diagnostics = await GenerateAsync(CodeFixTestHost.CreateProject(Source, Files));

        diagnostics.Select(d => d.Properties["Key"]).ShouldBe(["a", "item", "list.0", "menu.title"]);

        var item = diagnostics[1];

        item.Properties["Language"].ShouldBe("de");
        item.Properties["SourceLanguage"].ShouldBe("en");
        item.Properties["SourceFile"].ShouldBe(EnglishPath);
        GeneratorTestHost.GetList(item.Properties, "EntryKey").ShouldBe(["item_one", "item_other"]);
        GeneratorTestHost.GetList(item.Properties, "EntryValue").ShouldBe(["{{count}} item", "{{count}} items"]);
    }

    [Fact]
    public async Task Fix_MissingKeys_ShouldOfferEachKeyAndAllKeys()
    {
        var actions = await GetActionsAsync(CodeFixTestHost.CreateProject(Source, Files));

        actions.Select(a => a.Title).ShouldBe([
            "Add 'a' to the 'de' translation file",
            "Add 'item' to the 'de' translation file",
            "Add 'menu.title' to the 'de' translation file",
            "Add all 3 missing keys to the 'de' translation file"
        ]);
    }

    [Fact]
    public async Task Fix_MissingKey_ShouldCopyTheSourceTextInTheSourceOrder()
    {
        var actions = await GetActionsAsync(CodeFixTestHost.CreateProject(Source, Files));

        var solution = await CodeFixTestHost.ApplyAsync(actions[0]);

        (await CodeFixTestHost.GetTextAsync(solution, GermanPath)).ShouldBe("{\n  \"a\": \"A {{x}}\",\n  \"b\": \"Bé\"\n}");
    }

    [Fact]
    public async Task Fix_AllMissingKeys_ShouldAddEveryKeyExceptArrays()
    {
        var actions = await GetActionsAsync(CodeFixTestHost.CreateProject(Source, Files));

        var solution = await CodeFixTestHost.ApplyAsync(actions[^1]);

        (await CodeFixTestHost.GetTextAsync(solution, GermanPath)).ShouldBe(
            "{\n  \"a\": \"A {{x}}\",\n  \"b\": \"Bé\",\n  \"item_one\": \"{{count}} item\",\n  \"item_other\": \"{{count}} items\",\n  \"menu\": {\n    \"title\": \"T\"\n  }\n}");
    }

    [Fact]
    public async Task Fix_SourceFileNotInProject_ShouldNotOfferFixes()
    {
        var project = CodeFixTestHost.CreateProject(Source, Files);
        var diagnostics = (await GenerateAsync(project)).ToImmutableArray();
        var withoutSource = project.RemoveAdditionalDocument(project.AdditionalDocuments.Single(d => d.FilePath == EnglishPath).Id);

        var actions = await CodeFixTestHost.GetActionsAsync(new MissingKeyCodeFixProvider(), withoutSource, diagnostics);

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_KeysAddedMeanwhile_ShouldNotOfferFixes()
    {
        var project = CodeFixTestHost.CreateProject(Source, Files);
        var diagnostics = (await GenerateAsync(project)).Where(d => d.Properties["Key"] == "a").ToImmutableArray();
        var german = project.AdditionalDocuments.Single(d => d.FilePath == GermanPath);
        var updated = project.Solution.WithAdditionalDocumentText(german.Id, SourceText.From("{ \"a\": \"A\", \"b\": \"B\" }")).GetProject(project.Id);

        var actions = await CodeFixTestHost.GetActionsAsync(new MissingKeyCodeFixProvider(), updated, diagnostics);

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_DiagnosticWithoutProperties_ShouldNotOfferFixes()
    {
        var project = CodeFixTestHost.CreateProject(Source, Files);
        var location = (await GenerateAsync(project))[0].Location;
        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("I18N002", "Missing translation", "Missing", "I18Next", DiagnosticSeverity.Warning, true),
            location);

        var actions = await CodeFixTestHost.GetActionsAsync(new MissingKeyCodeFixProvider(), project, [diagnostic]);

        actions.ShouldBeEmpty();
        new MissingKeyCodeFixProvider().GetFixAllProvider().ShouldBeNull();
    }

    private static async Task<List<CodeAction>> GetActionsAsync(Project project)
    {
        return await CodeFixTestHost.GetActionsAsync(new MissingKeyCodeFixProvider(), project, [.. await GenerateAsync(project)]);
    }

    private static async Task<List<Diagnostic>> GenerateAsync(Project project)
    {
        return (await CodeFixTestHost.GenerateAsync(project)).Where(d => d.Id == "I18N002").ToList();
    }
}
