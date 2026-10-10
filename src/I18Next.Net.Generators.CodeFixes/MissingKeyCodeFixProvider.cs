using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace I18Next.Net.Generators.CodeFixes;

/// <summary>
///     Adds the keys missing in a language with the text of the source language, which is the text shown until they are translated.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingKeyCodeFixProvider), DocumentKinds = [nameof(TextDocumentKind.AdditionalDocument)],
    DocumentExtensions = [".json"])]
[Shared]
public sealed class MissingKeyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("I18N002");

    public override FixAllProvider GetFixAllProvider()
    {
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.TextDocument;
        var referenceTexts = new Dictionary<string, string>();
        var fixes = new List<(Diagnostic Diagnostic, List<KeyValuePair<string, string>> Entries, string ReferenceText)>();

        foreach (var diagnostic in context.Diagnostics)
        {
            var properties = diagnostic.Properties;

            if (!properties.TryGetValue(DiagnosticProperties.Key, out var key)
                || !properties.TryGetValue(DiagnosticProperties.SourceFile, out var sourceFile))
                continue;

            if (!referenceTexts.TryGetValue(sourceFile, out var referenceText))
            {
                var source = TranslationFiles.Find(document.Project, sourceFile);

                referenceText = source == null ? null : (await source.GetTextAsync(context.CancellationToken).ConfigureAwait(false)).ToString();
                referenceTexts[sourceFile] = referenceText;
            }

            if (referenceText == null)
                continue;

            var entries = DiagnosticProperties.GetList(properties, DiagnosticProperties.EntryKeys)
                .Zip(DiagnosticProperties.GetList(properties, DiagnosticProperties.EntryValues), (k, v) => new KeyValuePair<string, string>(k, v))
                .Where(e => !JsonKeyEditor.IsInArray(referenceText, e.Key))
                .ToList();

            var solution = await TranslationFiles.AddKeysAsync(document.Project.Solution, document.Id, entries, referenceText, context.CancellationToken)
                .ConfigureAwait(false);

            if (solution == null)
                continue;

            fixes.Add((diagnostic, entries, referenceText));

            context.RegisterCodeFix(CodeAction.Create($"Add '{key}' to the '{properties[DiagnosticProperties.Language]}' translation file",
                _ => Task.FromResult(solution), "I18N002.Add." + key), diagnostic);
        }

        if (fixes.Count < 2)
            return;

        var allEntries = fixes.SelectMany(f => f.Entries);
        var allSolution = await TranslationFiles.AddKeysAsync(document.Project.Solution, document.Id, allEntries, fixes[0].ReferenceText, context.CancellationToken)
            .ConfigureAwait(false);
        var title = $"Add all {fixes.Count} missing keys to the '{fixes[0].Diagnostic.Properties[DiagnosticProperties.Language]}' translation file";

        context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(allSolution), "I18N002.AddAll"),
            fixes.Select(f => f.Diagnostic).ToImmutableArray());
    }
}
