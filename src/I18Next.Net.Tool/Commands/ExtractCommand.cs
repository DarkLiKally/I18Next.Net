using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Tool.Extraction;
using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

internal static class ExtractCommand
{
    public static Command Create()
    {
        var locales = new LocalesOptions();
        var sources = new SourceOptions("The C#, Razor and cshtml files or directories to scan.", true);
        var defaultValue = new Option<string>("--default-value")
        {
            Description = "The value of added keys: an empty string or the key.",
            DefaultValueFactory = _ => "empty"
        };
        defaultValue.AcceptOnlyFromAmong("empty", "key");
        var removeUnused = new Option<bool>("--remove-unused") { Description = "Removes keys which are not used in the source code." };
        var check = new Option<bool>("--check", "--dry-run") { Description = "Does not write the files and fails when they would change." };

        var command = new Command("extract", "Adds the translation keys used in the source code to the translation files of all languages.");
        locales.AddTo(command);
        sources.AddTo(command);
        command.Options.Add(defaultValue);
        command.Options.Add(removeUnused);
        command.Options.Add(check);

        command.SetToolAction((parseResult, output, _) =>
        {
            var files = locales.GetFiles(parseResult);

            if (files.Languages.Count == 0)
                throw new ToolException($"No language directories found in {files.Path}, add them or pass --languages.");

            var extractionOptions = sources.GetOptions(parseResult);
            var extractor = new KeyExtractor(extractionOptions);
            var keys = extractor.Extract(parseResult.GetValue(sources.Sources)).Where(k => files.HasNamespace(k.Namespace)).ToList();
            var settings = new ExtractSettings(extractionOptions.NamespaceSeparator, parseResult.GetValue(defaultValue) == "key",
                parseResult.GetValue(removeUnused), parseResult.GetValue(check));

            output.WriteLine($"Found {keys.Select(k => (k.Namespace, k.Key)).Distinct().Count()} keys in {extractor.FileCount} files.");

            return Task.FromResult(Update(files, keys, extractor.GeneratedMemberChains, settings, output));
        });

        return command;
    }

    private static int Update(TranslationFiles files, List<ExtractedKey> keys, IReadOnlyCollection<string> generatedMemberChains, ExtractSettings settings,
        TextWriter output)
    {
        var usage = new KeyUsage(files.KeySeparator, settings.NamespaceSeparator);
        usage.AddRange(keys);
        usage.AddGeneratedMemberChains(generatedMemberChains);

        var namespaces = keys.Select(k => k.Namespace);

        if (settings.RemoveUnused)
        {
            namespaces = namespaces.Concat(files.Namespaces);

            foreach (var file in files.LoadExisting())
            {
                foreach (var entry in file.Document.GetEntries())
                    usage.AddNestings(file.Namespace, entry.Value);
            }
        }

        var changed = 0;

        foreach (var language in files.Languages)
        {
            foreach (var @namespace in namespaces.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal))
            {
                var file = files.Load(language, @namespace);
                var added = 0;
                var removed = 0;

                foreach (var key in keys.Where(k => k.Namespace == @namespace && !k.ReturnsObject).SelectMany(k => k.GetKeys(language)).Distinct())
                {
                    if (file.Document.ContainsKey(key))
                        continue;

                    if (file.Document.SetValue(key, settings.KeyAsDefaultValue ? key : string.Empty))
                        added++;
                    else
                        output.WriteLine($"{file.Path}: cannot add {key}, a parent key is not an object.");
                }

                if (settings.RemoveUnused)
                {
                    foreach (var entry in file.Document.GetEntries().ToList())
                    {
                        if (!usage.IsUsed(@namespace, entry.Key) && file.Document.Remove(entry.Key))
                            removed++;
                    }
                }

                if (added + removed == 0 || !file.Save(settings.Check))
                    continue;

                changed++;
                output.WriteLine(settings.Check
                    ? $"{file.Path}: would add {added} and remove {removed} keys."
                    : $"{file.Path}: added {added} and removed {removed} keys.");
            }
        }

        if (changed == 0)
        {
            output.WriteLine("The translation files are up to date.");

            return ExitCodes.Success;
        }

        return settings.Check ? ExitCodes.ProblemsFound : ExitCodes.Success;
    }

    private sealed class ExtractSettings(string namespaceSeparator, bool keyAsDefaultValue, bool removeUnused, bool check)
    {
        public string NamespaceSeparator { get; } = namespaceSeparator;

        public bool KeyAsDefaultValue { get; } = keyAsDefaultValue;

        public bool RemoveUnused { get; } = removeUnused;

        public bool Check { get; } = check;
    }
}
