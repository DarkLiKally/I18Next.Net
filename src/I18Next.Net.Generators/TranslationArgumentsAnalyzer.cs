using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace I18Next.Net.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TranslationArgumentsAnalyzer : DiagnosticAnalyzer
{
    private const string NestingPrefix = "$t(";

    private static readonly HashSet<string> MethodNames = ["T", "Ta"];

    private static readonly HashSet<string> ReservedNames =
    [
        "count", "context", "ordinal", "defaultValue", "lng", "lngs", "fallbackLng", "ns", "keyPrefix", "keySeparator", "nsSeparator", "formatParams",
        "joinArrays", "returnObjects", "returnDetails", "replace", "interpolation", "interpolate", "nest", "skipInterpolation", "postProcess", "sprintf"
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.MissingArgument, Diagnostics.UnusedArgument);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var i18NextType = context.Compilation.GetTypeByMetadataName("I18Next.Net.II18Next");
        var localizerType = context.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Localization.IStringLocalizer");

        if (i18NextType == null && localizerType == null)
            return;

        var catalog = ResourceCatalog.Create(context.Compilation, context.Options, context.CancellationToken);

        if (catalog == null)
            return;

        context.RegisterOperationAction(c => AnalyzeInvocation(c, i18NextType, localizerType, catalog), OperationKind.Invocation);

        if (localizerType != null)
            context.RegisterOperationAction(c => AnalyzeIndexer(c, localizerType, catalog), OperationKind.PropertyReference);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol i18NextType, INamedTypeSymbol localizerType,
        ResourceCatalog catalog)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (i18NextType != null && MethodNames.Contains(method.Name) && TranslationCall.Implements(method.ContainingType, i18NextType))
        {
            Analyze(context, catalog, TranslationCall.FromInvocation(invocation, catalog.Target));
        }
        else if (localizerType != null && method.Name == "GetString" && TranslationCall.Implements(GetReceiverType(method), localizerType))
        {
            Analyze(context, catalog, TranslationCall.FromLocalizer(invocation.Arguments, catalog.Target));
        }
    }

    private static ITypeSymbol GetReceiverType(IMethodSymbol method)
    {
        return method.IsExtensionMethod ? method.Parameters[0].Type : method.ContainingType;
    }

    private static void AnalyzeIndexer(OperationAnalysisContext context, INamedTypeSymbol localizerType, ResourceCatalog catalog)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;

        if (reference.Property.IsIndexer && TranslationCall.Implements(reference.Property.ContainingType, localizerType))
            Analyze(context, catalog, TranslationCall.FromLocalizer(reference.Arguments, catalog.Target));
    }

    private static void Analyze(OperationAnalysisContext context, ResourceCatalog catalog, TranslationCall call)
    {
        if (call?.Arguments is not IAnonymousObjectCreationOperation arguments)
            return;

        var sourceVariants = catalog.GetSourceVariants(call.Namespace, call.Key);

        if (sourceVariants.Count == 0)
            return;

        var options = GetMembers(arguments);
        var interpolation = arguments;

        if (options.ContainsKey("keyPrefix") || options.ContainsKey("ns"))
            return;

        if (options.TryGetValue("replace", out var replace))
        {
            if (TranslationCall.Unwrap(replace.Value) is not IAnonymousObjectCreationOperation replaceArguments)
                return;

            interpolation = replaceArguments;
        }

        var provided = GetMembers(interpolation);
        var variants = catalog.GetAllVariants(call.Namespace, call.Key).Select(e => e.Value).ToList();
        var used = new HashSet<string>(variants.SelectMany(ResourceModel.GetPlaceholders));
        var nesting = variants.Any(v => v.IndexOf(NestingPrefix, StringComparison.Ordinal) >= 0);
        var unused = provided.Keys.Where(n => !used.Contains(n) && !IsReserved(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();

        var missing = Select(sourceVariants, call.Key, options, catalog.Target.JsonFormatVersion)
            .SelectMany(ResourceModel.GetPlaceholders)
            .Where(p => !IsReserved(p) && !provided.ContainsKey(p))
            .Distinct()
            .ToList();

        var key = call.Prefix + call.Key;

        foreach (var placeholder in missing)
        {
            var properties = ImmutableDictionary.CreateBuilder<string, string>();
            var candidate = nesting ? null : FindCandidate(placeholder, unused, missing);

            properties[DiagnosticProperties.Placeholder] = placeholder;

            if (candidate != null)
                properties[DiagnosticProperties.Member] = candidate;

            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingArgument, interpolation.Syntax.GetLocation(), properties.ToImmutable(), key,
                placeholder));
        }

        if (nesting)
            return;

        foreach (var name in unused)
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnusedArgument, provided[name].Syntax.GetLocation(), name, key));
    }

    private static Dictionary<string, ISimpleAssignmentOperation> GetMembers(IAnonymousObjectCreationOperation creation)
    {
        var members = new Dictionary<string, ISimpleAssignmentOperation>();

        foreach (var initializer in creation.Initializers)
        {
            if (initializer is ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } assignment)
                members[property.Property.Name] = assignment;
        }

        return members;
    }

    /// <summary>
    ///     Returns the values which can be used for the options of a call: the values of a constant context, falling back to the key, and the plural
    ///     forms when a count is passed.
    /// </summary>
    private static List<string> Select(IReadOnlyList<ResourceEntry> variants, string key, Dictionary<string, ISimpleAssignmentOperation> options,
        int jsonFormatVersion)
    {
        var count = options.ContainsKey("count");
        var ordinal = options.TryGetValue("ordinal", out var ordinalOption) && ordinalOption.Value.ConstantValue is { HasValue: true, Value: true };

        if (options.TryGetValue("context", out var contextOption)
            && contextOption.Value.ConstantValue is { HasValue: true, Value: string { Length: > 0 } context })
        {
            var contextValues = SelectForStem(variants, key + "_" + context, count, ordinal, jsonFormatVersion);

            if (contextValues.Count > 0)
                return contextValues;
        }

        return SelectForStem(variants, key, count, ordinal, jsonFormatVersion);
    }

    private static List<string> SelectForStem(IReadOnlyList<ResourceEntry> variants, string stem, bool count, bool ordinal, int jsonFormatVersion)
    {
        var selected = new List<string>();

        if (count)
        {
            selected.AddRange(GetPluralForms(variants, stem, ordinal, jsonFormatVersion));

            if (selected.Count == 0 && ordinal)
                selected.AddRange(GetPluralForms(variants, stem, false, jsonFormatVersion));
        }

        if (selected.Count == 0 || jsonFormatVersion < 4)
            selected.AddRange(variants.Where(v => v.Key == stem).Select(v => v.Value));

        return selected;
    }

    private static IEnumerable<string> GetPluralForms(IReadOnlyList<ResourceEntry> variants, string stem, bool ordinal, int jsonFormatVersion)
    {
        foreach (var variant in variants)
        {
            if (ResourceModel.TryStripPluralSuffix(variant.Key, jsonFormatVersion, out var variantStem, out var variantOrdinal)
                && variantStem == stem
                && variantOrdinal == ordinal)
                yield return variant.Value;
        }
    }

    private static string FindCandidate(string placeholder, List<string> unused, List<string> missing)
    {
        if (!SyntaxFacts.IsValidIdentifier(placeholder))
            return null;

        var candidates = unused.Where(m => IsSimilar(placeholder, m)).ToList();

        if (candidates.Count != 1 || missing.Any(p => p != placeholder && IsSimilar(p, candidates[0])))
            return null;

        return candidates[0];
    }

    private static bool IsSimilar(string placeholder, string member)
    {
        var a = placeholder.ToLowerInvariant();
        var b = member.ToLowerInvariant();
        var shorter = Math.Min(a.Length, b.Length);

        if (shorter >= 3 && (a.Contains(b) || b.Contains(a)))
            return true;

        var maximum = Math.Max(1, shorter / 3);

        return StringDistance.Get(a, b, maximum) <= maximum;
    }

    private static bool IsReserved(string name)
    {
        return ReservedNames.Contains(name) || name.StartsWith("defaultValue", StringComparison.Ordinal);
    }
}
