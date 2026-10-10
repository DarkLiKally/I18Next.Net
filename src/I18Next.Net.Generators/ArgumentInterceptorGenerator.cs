using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace I18Next.Net.Generators;

/// <summary>
///     Converts anonymous objects passed as translation arguments into dictionaries at compile time, so trimmed and Native
///     AOT applications don't depend on reflection to read their properties.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ArgumentInterceptorGenerator : IIncrementalGenerator
{
    public const string GeneratedNamespace = "I18Next.Net.Generated";

    private const string Dictionary = "global::System.Collections.Generic.Dictionary<string, object>";

    private static readonly HashSet<string> MethodNames = ["T", "Ta", "TObject", "TaObject", "Exists", "ExistsAsync"];

    private static readonly Lazy<MethodInfo> GetInterceptableLocationMethod = new(() => typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).GetMethod("GetInterceptableLocation",
        [typeof(SemanticModel), typeof(InvocationExpressionSyntax), typeof(CancellationToken)]));

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabled = context.AnalyzerConfigOptionsProvider.Select(static (options, _) => IsEnabled(options.GlobalOptions));

        var callSites = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => IsCandidate(node), static (syntax, cancellationToken) => CreateCallSite(syntax, cancellationToken))
            .Where(static c => c != null)
            .Collect();

        context.RegisterSourceOutput(callSites.Combine(enabled), static (production, source) => Execute(production, source.Left, source.Right));
    }

    private static bool IsEnabled(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue("build_property.I18NextInterceptArguments", out var value) && bool.TryParse(value, out var enabled))
            return enabled;

        return IsTrue(options, "build_property.PublishAot") || IsTrue(options, "build_property.PublishTrimmed") || IsTrue(options, "build_property.IsAotCompatible");
    }

    private static bool IsTrue(AnalyzerConfigOptions options, string key)
    {
        return options.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } invocation || invocation.ArgumentList.Arguments.Count < 2)
            return false;

        var name = memberAccess.Name switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null
        };

        return name != null && MethodNames.Contains(name) && invocation.ArgumentList.Arguments.Any(a => a.Expression is AnonymousObjectCreationExpressionSyntax);
    }

    private static CallSite CreateCallSite(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
            return null;

        var method = operation.TargetMethod;

        if (method.IsStatic || method.IsExtensionMethod || !IsTranslationType(method.ContainingType))
            return null;

        var argsArgument = operation.Arguments.FirstOrDefault(a => a.Parameter?.Name == "args");

        if (argsArgument?.Parameter?.Type.SpecialType != SpecialType.System_Object ||
            (argsArgument.Value as IConversionOperation)?.Operand.Type is not { IsAnonymousType: true } argsType)
            return null;

        var compilation = context.SemanticModel.Compilation;

        if (!CanBeNamed(argsType, compilation))
            return null;

        var attribute = GetInterceptsLocationAttribute(context.SemanticModel, invocation, cancellationToken);

        if (attribute == null)
            return null;

        return new CallSite(GetSignature(method), GetCall(method), GetExample(argsType), GetDictionary(argsType, "value"), attribute);
    }

    private static bool IsTranslationType(INamedTypeSymbol type)
    {
        return IsI18NextType(type, "II18Next") || IsI18NextType(type, "FixedT") || type.AllInterfaces.Any(i => IsI18NextType(i, "II18Next"));
    }

    private static bool IsI18NextType(INamedTypeSymbol type, string name)
    {
        return type.Name == name && type.ContainingNamespace?.ToDisplayString() == "I18Next.Net";
    }

    private static bool CanBeNamed(ITypeSymbol type, Compilation compilation, bool allowAnonymous = true)
    {
        switch (type)
        {
            case { IsAnonymousType: true }:
                return allowAnonymous && GetProperties(type).All(p => CanBeNamed(p.Type, compilation));
            case ITypeParameterSymbol or IErrorTypeSymbol or IPointerTypeSymbol or IFunctionPointerTypeSymbol or IDynamicTypeSymbol:
                return false;
            case IArrayTypeSymbol array:
                return CanBeNamed(array.ElementType, compilation, false);
            case INamedTypeSymbol named:
                return compilation.IsSymbolAccessibleWithin(named, compilation.Assembly) && named.TypeArguments.All(t => CanBeNamed(t, compilation, false));
            default:
                return false;
        }
    }

    private static string GetInterceptsLocationAttribute(SemanticModel semanticModel, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var location = GetInterceptableLocationMethod.Value?.Invoke(null, [semanticModel, invocation, cancellationToken]);

        if (location?.GetType().GetProperty("Version")?.GetValue(location) is not int version ||
            location.GetType().GetProperty("Data")?.GetValue(location) is not string data)
            return null;

        return $"[global::System.Runtime.CompilerServices.InterceptsLocation({version}, {NameScope.ToLiteral(data)})]";
    }

    private static string GetSignature(IMethodSymbol method)
    {
        var definition = method.OriginalDefinition;
        var typeParameters = definition.TypeParameters.Length == 0 ? "" : $"<{string.Join(", ", definition.TypeParameters.Select(t => t.Name))}>";
        var parameters = definition.Parameters.Select(p => $"{TypeName(p.Type)} {Identifier(p.Name)}");

        return $"{TypeName(definition.ReturnType)} {{0}}{typeParameters}(this {TypeName(definition.ContainingType)} @this, {string.Join(", ", parameters)})";
    }

    private static string GetCall(IMethodSymbol method)
    {
        var definition = method.OriginalDefinition;
        var typeArguments = definition.TypeParameters.Length == 0 ? "" : $"<{string.Join(", ", definition.TypeParameters.Select(t => t.Name))}>";
        var arguments = definition.Parameters.Select(p => p.Name == "args" ? "{0}" : Identifier(p.Name));

        return $"@this.{definition.Name}{typeArguments}({string.Join(", ", arguments)})";
    }

    private static string GetExample(ITypeSymbol type)
    {
        if (!type.IsAnonymousType)
            return $"default({TypeName(type)})";

        return $"new {{ {string.Join(", ", GetProperties(type).Select(p => $"{Identifier(p.Name)} = {GetExample(p.Type)}"))} }}";
    }

    private static string GetDictionary(ITypeSymbol type, string expression)
    {
        var properties = GetProperties(type).ToList();
        var entries = properties.Select(p =>
        {
            var value = $"{expression}.{Identifier(p.Name)}";

            return $"[{NameScope.ToLiteral(p.Name)}] = {(p.Type.IsAnonymousType ? $"{value} == null ? null : {GetDictionary(p.Type, value)}" : value)}";
        });

        return $"new {Dictionary}({properties.Count}) {{ {string.Join(", ", entries)} }}";
    }

    private static IEnumerable<IPropertySymbol> GetProperties(ITypeSymbol type)
    {
        return type.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic && !p.IsIndexer);
    }

    private static string TypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string Identifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<CallSite> callSites, bool enabled)
    {
        if (!enabled || callSites.IsDefaultOrEmpty)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine();
        builder.AppendLine("namespace System.Runtime.CompilerServices");
        builder.AppendLine("{");
        builder.AppendLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
        builder.AppendLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute");
        builder.AppendLine("    {");
        builder.AppendLine("        public InterceptsLocationAttribute(int version, string data)");
        builder.AppendLine("        {");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"namespace {GeneratedNamespace}");
        builder.AppendLine("{");
        builder.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"I18Next.Net.Generators\", \"1.0.0\")]");
        builder.AppendLine("    file static class I18NextArgumentInterceptors");
        builder.AppendLine("    {");

        var conversions = new Dictionary<(string Example, string Dictionary), int>();
        var groups = callSites
            .GroupBy(c => (c.Signature, c.Call, c.Example, c.Dictionary))
            .OrderBy(g => g.Key.Signature, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Example, StringComparer.Ordinal);
        var index = 0;

        foreach (var group in groups)
        {
            var conversionKey = (group.Key.Example, group.Key.Dictionary);

            if (!conversions.TryGetValue(conversionKey, out var conversion))
                conversions[conversionKey] = conversion = conversions.Count;

            foreach (var attribute in group.Select(c => c.Attribute).Distinct().OrderBy(a => a, StringComparer.Ordinal))
                builder.AppendLine($"        {attribute}");

            builder.AppendLine($"        public static {string.Format(group.Key.Signature, "Intercept" + index++)}");
            builder.AppendLine("        {");
            builder.AppendLine($"            return {string.Format(group.Key.Call, $"ToDictionary{conversion}(args)")};");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        foreach (var conversion in conversions.OrderBy(c => c.Value))
        {
            builder.AppendLine($"        private static {Dictionary} ToDictionary{conversion.Value}(object args)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (args == null)");
            builder.AppendLine("                return null;");
            builder.AppendLine();
            builder.AppendLine($"            var value = Cast(args, {conversion.Key.Example});");
            builder.AppendLine();
            builder.AppendLine($"            return {conversion.Key.Dictionary};");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        private static T Cast<T>(object value, T example) => (T)value;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("I18Next.ArgumentInterceptors.g.cs", builder.ToString());
    }

    private sealed class CallSite(string signature, string call, string example, string dictionary, string attribute) : IEquatable<CallSite>
    {
        public string Attribute { get; } = attribute;

        public string Call { get; } = call;

        public string Dictionary { get; } = dictionary;

        public string Example { get; } = example;

        public string Signature { get; } = signature;

        public bool Equals(CallSite other)
        {
            return other != null && Attribute == other.Attribute && Call == other.Call && Dictionary == other.Dictionary && Example == other.Example &&
                   Signature == other.Signature;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CallSite);
        }

        public override int GetHashCode()
        {
            return Attribute.GetHashCode();
        }
    }
}
