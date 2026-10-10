using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace I18Next.Net.Generators;

internal sealed class TranslationCall
{
    private TranslationCall(IOperation keyOperation, string key, string @namespace, string prefix, IOperation arguments)
    {
        KeyOperation = keyOperation;
        Key = key;
        Namespace = @namespace;
        Prefix = prefix;
        Arguments = arguments;
    }

    /// <summary>
    ///     The arguments passed to the translation without implicit conversions, or <c>null</c>.
    /// </summary>
    public IOperation Arguments { get; }

    public string Key { get; }

    public IOperation KeyOperation { get; }

    public string Namespace { get; }

    /// <summary>
    ///     The namespace and separator written in front of the key, or an empty string.
    /// </summary>
    public string Prefix { get; }

    public static TranslationCall FromInvocation(IInvocationOperation invocation, ResourceTarget target)
    {
        IOperation key = null;
        IOperation arguments = null;
        var defaultNamespace = target.DefaultNamespace;

        foreach (var argument in invocation.Arguments)
        {
            switch (argument.Parameter?.Name)
            {
                case "key":
                    key = argument.Value;
                    break;
                case "args":
                    arguments = argument.Value;
                    break;
                case "defaultNamespace" when argument.Value.ConstantValue is { HasValue: true, Value: string value }:
                    defaultNamespace = value;
                    break;
            }
        }

        return Create(key, defaultNamespace, arguments, target);
    }

    public static TranslationCall FromLocalizer(ImmutableArray<IArgumentOperation> arguments, ResourceTarget target)
    {
        var key = arguments.FirstOrDefault(a => a.Parameter?.Name == "name")?.Value;
        var values = arguments.FirstOrDefault(a => a.Parameter?.Name == "arguments")?.Value;
        IOperation args = null;

        if (values is IArrayCreationOperation { Initializer.ElementValues.Length: 1 } array)
            args = array.Initializer.ElementValues[0];

        return Create(key, target.DefaultNamespace, args, target);
    }

    public static bool Implements(ITypeSymbol type, INamedTypeSymbol interfaceType)
    {
        return SymbolEqualityComparer.Default.Equals(type, interfaceType)
               || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
    }

    public static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;

        return operation;
    }

    private static TranslationCall Create(IOperation keyOperation, string defaultNamespace, IOperation arguments, ResourceTarget target)
    {
        if (keyOperation?.ConstantValue is not { HasValue: true, Value: string key })
            return null;

        var @namespace = defaultNamespace;
        var prefix = "";
        var separator = target.NamespaceSeparator;

        if (!string.IsNullOrEmpty(separator))
        {
            var index = key.IndexOf(separator, StringComparison.Ordinal);

            if (index > 0)
            {
                @namespace = key.Substring(0, index);
                prefix = key.Substring(0, index + separator.Length);
                key = key.Substring(index + separator.Length);
            }
        }

        return new TranslationCall(keyOperation, key, @namespace, prefix, arguments == null ? null : Unwrap(arguments));
    }
}
