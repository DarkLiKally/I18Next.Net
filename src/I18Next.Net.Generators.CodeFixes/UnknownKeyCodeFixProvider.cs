using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace I18Next.Net.Generators.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnknownKeyCodeFixProvider))]
[Shared]
public sealed class UnknownKeyCodeFixProvider : CodeFixProvider
{
    private const string AddToSourceFileKey = "I18N010.AddToSourceFile";
    private const string AddToAllFilesKey = "I18N010.AddToAllFiles";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("I18N010");

    public override FixAllProvider GetFixAllProvider()
    {
        return new AddKeysFixAllProvider();
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var properties = diagnostic.Properties;

            if (root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                foreach (var suggestion in DiagnosticProperties.GetList(properties, DiagnosticProperties.Suggestions))
                {
                    var replaced = Replace(document, root, literal, suggestion);

                    context.RegisterCodeFix(CodeAction.Create($"Change to '{suggestion}'", _ => Task.FromResult(replaced), "I18N010.Change." + suggestion),
                        diagnostic);
                }
            }

            if (!properties.TryGetValue(DiagnosticProperties.Key, out var key))
                continue;

            var keys = new[] { key };
            var sourceFile = properties[DiagnosticProperties.SourceFile];
            var files = DiagnosticProperties.GetList(properties, DiagnosticProperties.Files);
            var solution = await AddKeysAsync(document.Project, sourceFile, [], keys, context.CancellationToken).ConfigureAwait(false);

            if (solution == null)
                continue;

            context.RegisterCodeFix(CodeAction.Create($"Add '{key}' to the '{properties[DiagnosticProperties.SourceLanguage]}' translation file",
                _ => Task.FromResult(solution), AddToSourceFileKey), diagnostic);

            if (files.Count == 0)
                continue;

            var allSolution = await AddKeysAsync(document.Project, sourceFile, files, keys, context.CancellationToken).ConfigureAwait(false);

            context.RegisterCodeFix(CodeAction.Create($"Add '{key}' to all translation files", _ => Task.FromResult(allSolution), AddToAllFilesKey),
                diagnostic);
        }
    }

    private static Document Replace(Document document, SyntaxNode root, LiteralExpressionSyntax literal, string key)
    {
        var replacement = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(key)).WithTriviaFrom(literal);

        return document.WithSyntaxRoot(root.ReplaceNode(literal, replacement));
    }

    private static async Task<Solution> AddKeysAsync(Project project, string sourceFile, IEnumerable<string> files, IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        var source = TranslationFiles.Find(project, sourceFile);

        if (source == null)
            return null;

        var entries = keys.Select(k => new KeyValuePair<string, string>(k, k.Substring(k.LastIndexOf('.') + 1))).ToList();
        var solution = await TranslationFiles.AddKeysAsync(project.Solution, source.Id, entries, null, cancellationToken).ConfigureAwait(false);

        if (solution == null)
            return null;

        var referenceText = (await solution.GetAdditionalDocument(source.Id).GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();

        foreach (var file in files)
        {
            var document = TranslationFiles.Find(project, file);

            if (document == null)
                continue;

            solution = await TranslationFiles.AddKeysAsync(solution, document.Id, entries, referenceText, cancellationToken).ConfigureAwait(false)
                       ?? solution;
        }

        return solution;
    }

    private sealed class AddKeysFixAllProvider : FixAllProvider
    {
        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            var all = fixAllContext.CodeActionEquivalenceKey == AddToAllFilesKey;

            if (!all && fixAllContext.CodeActionEquivalenceKey != AddToSourceFileKey)
                return null;

            var diagnostics = new List<(Project Project, Diagnostic Diagnostic)>();

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document when fixAllContext.Document != null:
                    diagnostics.AddRange((await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false))
                        .Select(d => (fixAllContext.Project, d)));
                    break;
                case FixAllScope.Project:
                    diagnostics.AddRange((await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false))
                        .Select(d => (fixAllContext.Project, d)));
                    break;
                case FixAllScope.Solution:
                    foreach (var project in fixAllContext.Solution.Projects)
                    {
                        diagnostics.AddRange((await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false))
                            .Select(d => (project, d)));
                    }

                    break;
            }

            var groups = diagnostics
                .Where(d => d.Diagnostic.Properties.ContainsKey(DiagnosticProperties.Key))
                .GroupBy(d => (d.Project.Id, SourceFile: d.Diagnostic.Properties[DiagnosticProperties.SourceFile]))
                .ToList();

            if (groups.Count == 0)
                return null;

            return CodeAction.Create(all ? "Add the keys to all translation files" : "Add the keys to the translation files of the source language",
                async cancellationToken =>
                {
                    var solution = fixAllContext.Solution;

                    foreach (var group in groups)
                    {
                        var keys = group.Select(d => d.Diagnostic.Properties[DiagnosticProperties.Key]).Distinct().ToList();
                        var files = all
                            ? group.SelectMany(d => DiagnosticProperties.GetList(d.Diagnostic.Properties, DiagnosticProperties.Files)).Distinct().ToList()
                            : [];

                        solution = await AddKeysAsync(solution.GetProject(group.Key.Id), group.Key.SourceFile, files, keys, cancellationToken)
                            .ConfigureAwait(false) ?? solution;
                    }

                    return solution;
                }, fixAllContext.CodeActionEquivalenceKey);
        }
    }
}
