using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace I18Next.Net.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnknownKeyAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> MethodNames = ["T", "Ta", "TObject", "TaObject", "Exists", "ExistsAsync"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Diagnostics.UnknownKey);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var i18NextType = context.Compilation.GetTypeByMetadataName("I18Next.Net.II18Next");

        if (i18NextType == null)
            return;

        var catalog = ResourceCatalog.Create(context.Compilation, context.Options, context.CancellationToken);

        if (catalog == null)
            return;

        context.RegisterOperationAction(c => AnalyzeInvocation(c, i18NextType, catalog), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol i18NextType, ResourceCatalog catalog)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!MethodNames.Contains(method.Name) || !TranslationCall.Implements(method.ContainingType, i18NextType))
            return;

        var call = TranslationCall.FromInvocation(invocation, catalog.Target);

        if (call == null || !catalog.TryGetKeys(call.Namespace, out var keys) || keys.Contains(call.Key))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownKey, call.KeyOperation.Syntax.GetLocation(), GetProperties(catalog, call), call.Key,
            call.Namespace));
    }

    private static ImmutableDictionary<string, string> GetProperties(ResourceCatalog catalog, TranslationCall call)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string>();
        var sourceFile = catalog.GetSourceFile(call.Namespace);

        properties[DiagnosticProperties.Key] = call.Key;
        properties[DiagnosticProperties.SourceLanguage] = catalog.Target.SourceLanguage;
        properties[DiagnosticProperties.SourceFile] = sourceFile.Path;

        DiagnosticProperties.AddList(properties, DiagnosticProperties.Files, catalog.GetTranslationFiles(sourceFile).Select(f => f.Path));
        DiagnosticProperties.AddList(properties, DiagnosticProperties.Suggestions, KeySuggestions.Find(catalog, call));

        return properties.ToImmutable();
    }
}
