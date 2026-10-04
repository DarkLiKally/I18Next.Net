using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace I18Next.Net.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class I18NextResourcesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(ResourceTarget.AttributeName, static (_, _) => true, ResourceTarget.Create);
        var files = context.AdditionalTextsProvider.Select(ResourceFile.Create).Where(static f => f != null).Collect();

        context.RegisterSourceOutput(targets.Combine(files), static (production, source) => Execute(production, source.Left, source.Right));
    }

    private static void Execute(SourceProductionContext context, ResourceTarget target, ImmutableArray<ResourceFile> allFiles)
    {
        var files = allFiles.Where(f => f.IsIn(target.Path)).OrderBy(f => f.Path, System.StringComparer.Ordinal).ToList();

        foreach (var file in files.Where(f => f.Error != null))
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidFile, file.GetLocation(file.Error.Line, file.Error.Column), file.Path, file.Error.Message));

        var sourceFiles = files.Where(f => f.Language == target.SourceLanguage).ToList();

        if (sourceFiles.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoResources, target.GetLocation(), target.SourceLanguage, target.Path));
            return;
        }

        ReportLanguageDifferences(context, target, files, sourceFiles);

        var namespaces = sourceFiles
            .Select(f => ResourceModel.BuildNamespace(f.Namespace, f.Entries, target.JsonFormatVersion))
            .ToList();

        var hintName = (target.Namespace == null ? "" : target.Namespace + ".") + string.Join(".", target.ContainingTypes.Select(GetTypeName).Concat([target.Name]));

        context.AddSource($"{hintName}.I18Next.g.cs", SourceEmitter.Emit(target, namespaces));
    }

    private static void ReportLanguageDifferences(SourceProductionContext context, ResourceTarget target, List<ResourceFile> files,
        List<ResourceFile> sourceFiles)
    {
        var languages = files.Select(f => f.Language).Where(l => l != target.SourceLanguage).Distinct().OrderBy(l => l, System.StringComparer.Ordinal);

        foreach (var sourceFile in sourceFiles.Where(f => f.Error == null))
        {
            var sourceIndex = ResourceModel.GetKeyIndex(sourceFile.Entries, target.JsonFormatVersion);

            foreach (var language in languages)
            {
                var file = files.FirstOrDefault(f => f.Language == language && f.Namespace == sourceFile.Namespace);

                if (file == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingNamespace, target.GetLocation(), sourceFile.Namespace, language));
                    continue;
                }

                if (file.Error != null)
                    continue;

                var index = ResourceModel.GetKeyIndex(file.Entries, target.JsonFormatVersion);

                foreach (var entry in sourceIndex)
                {
                    if (!index.TryGetValue(entry.Key, out var placeholders))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingKey, file.GetLocation(), entry.Key, file.Namespace, language));
                        continue;
                    }

                    var unknown = placeholders.Where(p => !entry.Value.Contains(p)).OrderBy(p => p, System.StringComparer.Ordinal).ToList();

                    if (unknown.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownPlaceholder, file.GetLocation(), entry.Key, file.Namespace,
                            string.Join(", ", unknown.Select(p => "'" + p + "'")), language));
                    }
                }
            }
        }
    }

    private static string GetTypeName(string declaration)
    {
        var name = declaration.Substring(declaration.LastIndexOf(' ') + 1);
        var genericStart = name.IndexOf('<');

        return genericStart < 0 ? name : name.Substring(0, genericStart);
    }
}
