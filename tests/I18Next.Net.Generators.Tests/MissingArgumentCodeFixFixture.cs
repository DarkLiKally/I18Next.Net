using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Generators.CodeFixes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

using Shouldly;

using Xunit;

namespace I18Next.Net.Generators.Tests;

public class MissingArgumentCodeFixFixture
{
    private static readonly Dictionary<string, string> Files = new()
    {
        ["/app/locales/en/translation.json"] = """{ "greeting": "Hello {{name}}", "keyword": "{{class}} {{user}}" }"""
    };

    [Theory]
    [InlineData("""i18n.T("greeting", new { nam = "Jane" });""", """i18n.T("greeting", new { name = "Jane" });""")]
    [InlineData("""i18n.T("greeting", new { nam });""", """i18n.T("greeting", new { name = nam });""")]
    [InlineData("""i18n.T("greeting", new { person.Nam });""", """i18n.T("greeting", new { name = person.Nam });""")]
    [InlineData("""i18n.T("keyword", new { clas = 1, user = 2 });""", """i18n.T("keyword", new { @class = 1, user = 2 });""")]
    [InlineData("""i18n.T("greeting", new { replace = new { /* x */ nme = "Jane" } });""", """i18n.T("greeting", new { replace = new { /* x */ name = "Jane" } });""")]
    [InlineData("i18n.T(\"greeting\", new\n{\n    nam\n});", "i18n.T(\"greeting\", new\n{\n    name = nam\n});")]
    public async Task Fix_SimilarMember_ShouldRenameIt(string statement, string expected)
    {
        var project = CodeFixTestHost.CreateProject(Wrap(statement), Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single(d => d.Id == "I18N011");

        var action = (await CodeFixTestHost.GetActionsAsync(new MissingArgumentCodeFixProvider(), project, [diagnostic])).Single();
        var solution = await CodeFixTestHost.ApplyAsync(action);

        action.Title.ShouldStartWith("Rename '");
        (await CodeFixTestHost.GetTextAsync(solution, CodeFixTestHost.SourcePath)).ShouldBe(Wrap(expected));
    }

    [Fact]
    public async Task Fix_NoSimilarMember_ShouldNotOfferFix()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("greeting", new { other = 1 });"""), Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single(d => d.Id == "I18N011");

        var actions = await CodeFixTestHost.GetActionsAsync(new MissingArgumentCodeFixProvider(), project, [diagnostic]);

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_UnknownMember_ShouldKeepTheDocument()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("greeting", new { nam = 1 });"""), Files);
        var diagnostic = (await CodeFixTestHost.AnalyzeAsync(project)).Single(d => d.Id == "I18N011");
        var stale = Diagnostic.Create(diagnostic.Descriptor, diagnostic.Location, diagnostic.Properties.SetItem("Member", "other"));

        var action = (await CodeFixTestHost.GetActionsAsync(new MissingArgumentCodeFixProvider(), project, [stale])).Single();
        var solution = await CodeFixTestHost.ApplyAsync(action);

        action.Title.ShouldBe("Rename 'other' to 'name'");
        (await CodeFixTestHost.GetTextAsync(solution, CodeFixTestHost.SourcePath)).ShouldBe(Wrap("""i18n.T("greeting", new { nam = 1 });"""));
    }

    [Fact]
    public async Task Fix_DiagnosticOutsideAnonymousObject_ShouldNotOfferFix()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""i18n.T("greeting", new { nam = 1 });"""), Files);
        var unused = (await CodeFixTestHost.AnalyzeAsync(project)).Single(d => d.Id == "I18N012");
        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("I18N011", "Missing", "Missing", "I18Next", DiagnosticSeverity.Warning, true),
            unused.Location, ImmutableDictionary<string, string>.Empty.Add("Member", "nam").Add("Placeholder", "name"));

        var actions = await CodeFixTestHost.GetActionsAsync(new MissingArgumentCodeFixProvider(), project, [diagnostic]);

        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAll_SimilarMembers_ShouldRenameEveryMember()
    {
        var project = CodeFixTestHost.CreateProject(Wrap("""
                                                         i18n.T("greeting", new { nam = 1 });
                                                         i18n.T("keyword", new { clas = 1, usr = 2 });
                                                         """), Files);
        var diagnostics = (await CodeFixTestHost.AnalyzeAsync(project)).Where(d => d.Id == "I18N011").ToArray();
        var provider = new MissingArgumentCodeFixProvider();
        var context = new FixAllContext(project.Documents.Single(), provider, FixAllScope.Document, "I18N011.Rename", ["I18N011"],
            new CodeFixTestHost.DiagnosticProvider([.. diagnostics]), CancellationToken.None);

        var action = await provider.GetFixAllProvider().GetFixAsync(context);
        var solution = await CodeFixTestHost.ApplyAsync(action!);

        (await CodeFixTestHost.GetTextAsync(solution, CodeFixTestHost.SourcePath)).ShouldBe(Wrap("""
                                                                                                  i18n.T("greeting", new { name = 1 });
                                                                                                  i18n.T("keyword", new { @class = 1, user = 2 });
                                                                                                  """));
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
                         var nam = "Jane";
                         var person = new { Nam = "Jane" };
                 {{statements}}
                     }
                 }
                 """;
    }
}
