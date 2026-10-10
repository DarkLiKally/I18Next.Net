using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace I18Next.Net.Generators;

internal static class DiagnosticProperties
{
    public const string EntryKeys = "EntryKey";
    public const string EntryValues = "EntryValue";
    public const string Files = "File";
    public const string Key = "Key";
    public const string Language = "Language";
    public const string Member = "Member";
    public const string Placeholder = "Placeholder";
    public const string SourceFile = "SourceFile";
    public const string SourceLanguage = "SourceLanguage";
    public const string Suggestions = "Suggestion";

    public static void AddList(ImmutableDictionary<string, string>.Builder properties, string name, IEnumerable<string> values)
    {
        var index = 0;

        foreach (var value in values)
            properties[name + index++.ToString(CultureInfo.InvariantCulture)] = value;
    }

    public static List<string> GetList(ImmutableDictionary<string, string> properties, string name)
    {
        var values = new List<string>();

        while (properties.TryGetValue(name + values.Count.ToString(CultureInfo.InvariantCulture), out var value))
            values.Add(value);

        return values;
    }
}
