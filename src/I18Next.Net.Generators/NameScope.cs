using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;

namespace I18Next.Net.Generators;

internal sealed class NameScope(string enclosingTypeName, params string[] reserved)
{
    private readonly HashSet<string> _names = [enclosingTypeName, .. reserved];

    public string Add(string name)
    {
        var candidate = name;

        for (var i = 2; !_names.Add(candidate); i++)
            candidate = name + i.ToString(CultureInfo.InvariantCulture);

        return candidate;
    }

    public bool TryAdd(string name)
    {
        return _names.Add(name);
    }

    public static string ToPascalCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var upper = true;

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                upper = true;
                continue;
            }

            builder.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }

        if (builder.Length == 0)
            return "Key";

        return char.IsDigit(builder[0]) ? "_" + builder : builder.ToString();
    }

    public static string ToParameterName(string value)
    {
        var pascal = ToPascalCase(value);
        var name = pascal[0] == '_' ? pascal : char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);

        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    public static string ToLiteral(string value)
    {
        return SymbolDisplay.FormatLiteral(value, true);
    }
}
