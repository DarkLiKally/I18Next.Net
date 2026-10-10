using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace I18Next.Net.Generators.Tests;

internal static class CodeFixTestHost
{
    public const string SourcePath = "/app/Test.cs";

    public static Project CreateProject(string source, IDictionary<string, string> files)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Test", "Test", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                metadataReferences: GeneratorTestHost.References.Value))
            .AddDocument(DocumentId.CreateNewId(projectId), "Test.cs", SourceText.From(source), filePath: SourcePath);

        foreach (var file in files)
            solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(projectId), Path.GetFileName(file.Key), SourceText.From(file.Value), filePath: file.Key);

        return solution.GetProject(projectId);
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var analyzers = compilation!.WithAnalyzers([new UnknownKeyAnalyzer(), new TranslationArgumentsAnalyzer()], project.AnalyzerOptions);

        return [.. (await analyzers.GetAnalyzerDiagnosticsAsync()).OrderBy(d => d.Location.SourceSpan.Start)];
    }

    public static async Task<ImmutableArray<Diagnostic>> GenerateAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var texts = project.AnalyzerOptions.AdditionalFiles;
        var driver = CSharpGeneratorDriver.Create([new I18NextResourcesGenerator().AsSourceGenerator()], texts, new CSharpParseOptions(LanguageVersion.Latest));

        driver.RunGeneratorsAndUpdateCompilation(compilation!, out _, out var diagnostics);

        return diagnostics;
    }

    public static async Task<List<CodeAction>> GetActionsAsync(CodeFixProvider provider, Project project, ImmutableArray<Diagnostic> diagnostics)
    {
        var actions = new List<CodeAction>();
        var location = diagnostics[0].Location;
        var document = location.IsInSource
            ? project.GetDocument(location.SourceTree)
            : project.AdditionalDocuments.Single(d => d.FilePath == location.GetLineSpan().Path);

        var context = document is Document sourceDocument
            ? new CodeFixContext(sourceDocument, location.SourceSpan, diagnostics, (a, _) => actions.Add(a), CancellationToken.None)
            : new CodeFixContext(document!, location.SourceSpan, diagnostics, (a, _) => actions.Add(a), CancellationToken.None);

        await provider.RegisterCodeFixesAsync(context);

        return actions;
    }

    public static async Task<Solution> ApplyAsync(CodeAction action)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);

        return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
    }

    public static async Task<string> GetTextAsync(Solution solution, string path)
    {
        var document = solution.Projects.Single().AdditionalDocuments.SingleOrDefault(d => d.FilePath == path)
                       ?? (TextDocument)solution.Projects.Single().Documents.Single(d => d.FilePath == path);

        return (await document.GetTextAsync()).ToString();
    }

    public sealed class DiagnosticProvider(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<Diagnostic>());
        }
    }
}
