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
        var attributeType = context.Compilation.GetTypeByMetadataName(ResourceTarget.AttributeName);

        if (i18NextType == null || attributeType == null)
            return;

        var targets = FindTargets(context.Compilation.Assembly.GlobalNamespace, attributeType).ToList();

        if (targets.Count == 0)
            return;

        var files = context.Options.AdditionalFiles
            .Select(f => ResourceFile.Create(f, context.CancellationToken))
            .Where(f => f != null && f.Error == null)
            .ToList();

        var target = targets[0];
        var namespaces = new Dictionary<string, HashSet<string>>();

        foreach (var file in files.Where(f => f.Language == target.SourceLanguage && targets.Any(t => f.IsIn(t.Path))))
        {
            if (!namespaces.TryGetValue(file.Namespace, out var keys))
                namespaces[file.Namespace] = keys = [];

            keys.UnionWith(ResourceModel.GetResolvableKeys(file.Entries, target.JsonFormatVersion));
        }

        if (namespaces.Count == 0)
            return;

        context.RegisterOperationAction(c => AnalyzeInvocation(c, i18NextType, target, namespaces), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol i18NextType, ResourceTarget target,
        Dictionary<string, HashSet<string>> namespaces)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!MethodNames.Contains(method.Name) || !Implements(method.ContainingType, i18NextType))
            return;

        IArgumentOperation keyArgument = null;
        var defaultNamespace = target.DefaultNamespace;

        foreach (var argument in invocation.Arguments)
        {
            switch (argument.Parameter?.Name)
            {
                case "key":
                    keyArgument = argument;
                    break;
                case "defaultNamespace" when argument.Value.ConstantValue is { HasValue: true, Value: string value }:
                    defaultNamespace = value;
                    break;
            }
        }

        if (keyArgument?.Value.ConstantValue is not { HasValue: true, Value: string key })
            return;

        var @namespace = defaultNamespace;
        var separator = target.NamespaceSeparator;

        if (!string.IsNullOrEmpty(separator))
        {
            var index = key.IndexOf(separator, System.StringComparison.Ordinal);

            if (index > 0)
            {
                @namespace = key.Substring(0, index);
                key = key.Substring(index + separator.Length);
            }
        }

        if (!namespaces.TryGetValue(@namespace, out var keys) || keys.Contains(key))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownKey, keyArgument.Value.Syntax.GetLocation(), key, @namespace));
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol interfaceType)
    {
        return SymbolEqualityComparer.Default.Equals(type, interfaceType)
               || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
    }

    private static IEnumerable<ResourceTarget> FindTargets(INamespaceOrTypeSymbol container, INamedTypeSymbol attributeType)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceOrTypeSymbol child)
            {
                foreach (var target in FindTargets(child, attributeType))
                    yield return target;
            }

            if (member is not INamedTypeSymbol type)
                continue;

            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    yield return ResourceTarget.Create(type, attribute, Location.None);
            }
        }
    }
}
