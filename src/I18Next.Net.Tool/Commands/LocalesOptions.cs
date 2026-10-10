using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

/// <summary>
///     The options selecting the translation files shared by the commands.
/// </summary>
internal sealed class LocalesOptions
{
    public Option<string> Path { get; } = new("--path", "-p")
    {
        Description = "The directory with the translation files in the layout {lng}/{ns}.json.",
        DefaultValueFactory = _ => "locales"
    };

    public Option<string[]> Languages { get; } = new("--languages", "-l")
    {
        Description = "The languages to process, separated by commas or spaces. Defaults to all language directories.",
        AllowMultipleArgumentsPerToken = true
    };

    public Option<string[]> Namespaces { get; } = new("--namespaces", "-n")
    {
        Description = "The namespaces to process, separated by commas or spaces. Defaults to all namespace files.",
        AllowMultipleArgumentsPerToken = true
    };

    public Option<string> KeySeparator { get; } = new("--key-separator")
    {
        Description = "The separator of nested keys. Use an empty string or false for flat keys.",
        DefaultValueFactory = _ => "."
    };

    public static IReadOnlyList<string> SplitList(IEnumerable<string> values)
    {
        return (values ?? [])
            .SelectMany(v => v.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string GetKeySeparator(string value)
    {
        return value is null or "false" ? string.Empty : value;
    }

    public void AddTo(Command command)
    {
        command.Options.Add(Path);
        command.Options.Add(Languages);
        command.Options.Add(Namespaces);
        command.Options.Add(KeySeparator);
    }

    public TranslationFiles GetFiles(ParseResult parseResult)
    {
        return new TranslationFiles(
            parseResult.GetValue(Path),
            GetKeySeparator(parseResult.GetValue(KeySeparator)),
            SplitList(parseResult.GetValue(Languages)),
            SplitList(parseResult.GetValue(Namespaces)));
    }
}
