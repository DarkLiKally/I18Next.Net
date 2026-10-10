using System.CommandLine;

using I18Next.Net.Tool.Extraction;

namespace I18Next.Net.Tool.Commands;

/// <summary>
///     The options for finding translation keys in the source code.
/// </summary>
internal sealed class SourceOptions
{
    public SourceOptions(string sourceDescription, bool defaultToCurrentDirectory)
    {
        Sources = new Option<string[]>("--source", "-s")
        {
            Description = sourceDescription,
            AllowMultipleArgumentsPerToken = true
        };

        if (defaultToCurrentDirectory)
            Sources.DefaultValueFactory = _ => ["."];
    }

    public Option<string[]> Sources { get; }

    public Option<string> DefaultNamespace { get; } = new("--default-namespace")
    {
        Description = "The namespace of keys without namespace prefix.",
        DefaultValueFactory = _ => "translation"
    };

    public Option<string> NamespaceSeparator { get; } = new("--namespace-separator")
    {
        Description = "The separator between namespace and key, like in ns:key.",
        DefaultValueFactory = _ => ":"
    };

    public Option<string[]> Functions { get; } = new("--functions")
    {
        Description = "Additional names of translation methods taking the key as first string argument.",
        AllowMultipleArgumentsPerToken = true
    };

    public Option<string[]> Localizers { get; } = new("--localizers")
    {
        Description = "Additional names of variables whose indexer takes the key, besides names containing 'localizer'.",
        AllowMultipleArgumentsPerToken = true
    };

    public void AddTo(Command command)
    {
        command.Options.Add(Sources);
        command.Options.Add(DefaultNamespace);
        command.Options.Add(NamespaceSeparator);
        command.Options.Add(Functions);
        command.Options.Add(Localizers);
    }

    public ExtractionOptions GetOptions(ParseResult parseResult)
    {
        var options = new ExtractionOptions
        {
            DefaultNamespace = parseResult.GetValue(DefaultNamespace),
            NamespaceSeparator = parseResult.GetValue(NamespaceSeparator) ?? string.Empty
        };

        foreach (var function in LocalesOptions.SplitList(parseResult.GetValue(Functions)))
            options.FunctionNames.Add(function);

        foreach (var localizer in LocalesOptions.SplitList(parseResult.GetValue(Localizers)))
            options.LocalizerNames.Add(localizer);

        return options;
    }
}
