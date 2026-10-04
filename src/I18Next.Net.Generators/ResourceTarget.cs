using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace I18Next.Net.Generators;

internal sealed class ResourceTarget : IEquatable<ResourceTarget>
{
    public const string AttributeName = "I18Next.Net.I18NextResourcesAttribute";

    public string[] ContainingTypes { get; private set; }

    public string DefaultNamespace { get; private set; } = "translation";

    public string FilePath { get; private set; }

    public int JsonFormatVersion { get; private set; } = 4;

    public LinePositionSpan LineSpan { get; private set; }

    public string Name { get; private set; }

    public string Namespace { get; private set; }

    public string NamespaceSeparator { get; private set; } = ":";

    public string Path { get; private set; }

    public string SourceLanguage { get; private set; } = "en";

    public TextSpan Span { get; private set; }

    public bool SupportsExtensions { get; private set; }

    public string TypeDeclaration { get; private set; }

    public static ResourceTarget Create(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes[0];
        var location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? context.TargetNode.GetLocation();

        return Create(symbol, attribute, location, (TypeDeclarationSyntax)context.TargetNode);
    }

    public static ResourceTarget Create(INamedTypeSymbol symbol, AttributeData attribute, Location location, TypeDeclarationSyntax declaration = null)
    {
        var target = new ResourceTarget
        {
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            TypeDeclaration = GetDeclaration(symbol),
            ContainingTypes = GetContainingTypes(symbol),
            SupportsExtensions = symbol.IsStatic && symbol.ContainingType == null && symbol.TypeParameters.Length == 0,
            Path = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string ?? "" : "",
            FilePath = location.SourceTree?.FilePath,
            Span = location.SourceSpan,
            LineSpan = location.GetLineSpan().Span
        };

        foreach (var argument in attribute.NamedArguments)
        {
            switch (argument.Key)
            {
                case "SourceLanguage" when argument.Value.Value is string value:
                    target.SourceLanguage = value;
                    break;
                case "DefaultNamespace" when argument.Value.Value is string value:
                    target.DefaultNamespace = value;
                    break;
                case "NamespaceSeparator" when argument.Value.Value is string value:
                    target.NamespaceSeparator = value;
                    break;
                case "JsonFormatVersion" when argument.Value.Value is int value:
                    target.JsonFormatVersion = value;
                    break;
            }
        }

        return target;
    }

    public Location GetLocation()
    {
        return FilePath == null ? Location.None : Location.Create(FilePath, Span, LineSpan);
    }

    public bool Equals(ResourceTarget other)
    {
        return other != null
               && Name == other.Name
               && Namespace == other.Namespace
               && TypeDeclaration == other.TypeDeclaration
               && ContainingTypes.SequenceEqual(other.ContainingTypes)
               && SupportsExtensions == other.SupportsExtensions
               && Path == other.Path
               && SourceLanguage == other.SourceLanguage
               && DefaultNamespace == other.DefaultNamespace
               && NamespaceSeparator == other.NamespaceSeparator
               && JsonFormatVersion == other.JsonFormatVersion
               && FilePath == other.FilePath
               && Span == other.Span
               && LineSpan == other.LineSpan;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ResourceTarget);
    }

    public override int GetHashCode()
    {
        return (Namespace + "." + Name + "|" + Path).GetHashCode();
    }

    private static string[] GetContainingTypes(INamedTypeSymbol symbol)
    {
        var types = new List<string>();

        for (var type = symbol.ContainingType; type != null; type = type.ContainingType)
            types.Insert(0, GetDeclaration(type));

        return [.. types];
    }

    private static string GetDeclaration(INamedTypeSymbol symbol)
    {
        var keyword = symbol switch
        {
            { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class"
        };

        var name = symbol.TypeParameters.Length == 0 ? symbol.Name : $"{symbol.Name}<{string.Join(", ", symbol.TypeParameters.Select(p => p.Name))}>";

        return $"{(symbol.IsStatic ? "static " : "")}partial {keyword} {name}";
    }
}
