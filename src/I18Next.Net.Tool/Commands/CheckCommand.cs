using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Tool.Extraction;
using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

internal static class CheckCommand
{
    public static Command Create()
    {
        var locales = new LocalesOptions();
        var sources = new SourceOptions("The C#, Razor and cshtml files or directories to look for unused keys.", false);
        var reference = new Option<string>("--reference", "-r")
        {
            Description = "The language the other languages are compared with. Defaults to en or the first language."
        };

        var command = new Command("check",
            "Reports missing translations, empty values, placeholder mismatches and, with --source, unused keys. Fails when problems are found.");
        locales.AddTo(command);
        command.Options.Add(reference);
        sources.AddTo(command);

        command.SetToolAction((parseResult, output, _) =>
        {
            var files = locales.GetFiles(parseResult);
            files.EnsureLanguages();

            var referenceLanguage = parseResult.GetValue(reference) ?? (files.Languages.Contains("en") ? "en" : files.Languages[0]);

            if (!files.Languages.Contains(referenceLanguage))
                throw new ToolException($"The reference language {referenceLanguage} is not one of the languages {string.Join(", ", files.Languages)}.");

            var report = new CheckReport();
            var resources = files.Languages.ToDictionary(l => l, l => files.Namespaces.ToDictionary(n => n, n => LoadEntries(files, l, n)));

            output.WriteLine(files.Languages.Count == 1
                ? $"Checking {referenceLanguage}."
                : $"Comparing {string.Join(", ", files.Languages.Where(l => l != referenceLanguage))} with {referenceLanguage}.");

            foreach (var @namespace in files.Namespaces)
            {
                var referenceEntries = resources[referenceLanguage][@namespace];

                foreach (var language in files.Languages)
                {
                    var entries = resources[language][@namespace];

                    report.Empty.AddRange(entries.Where(e => e.Value.Length == 0).Select(e => $"{language}  {@namespace}:{e.Key}"));

                    if (language == referenceLanguage)
                        continue;

                    foreach (var key in GetMissingKeys(referenceEntries, entries, language))
                        report.Missing.Add($"{language}  {@namespace}:{key}");

                    foreach (var mismatch in GetPlaceholderMismatches(referenceEntries, entries))
                        report.Mismatches.Add($"{language}  {@namespace}:{mismatch}");
                }
            }

            var sourcePaths = parseResult.GetValue(sources.Sources);

            if (sourcePaths is { Length: > 0 })
                CheckUsage(files, referenceLanguage, resources, new KeyExtractor(sources.GetOptions(parseResult)), sourcePaths, report, parseResult.GetValue(sources.NamespaceSeparator));

            return Task.FromResult(report.Write(output));
        });

        return command;
    }

    private static Dictionary<string, string> LoadEntries(TranslationFiles files, string language, string @namespace)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in files.Load(language, @namespace).Document.GetEntries())
            result[entry.Key] = entry.Value;

        return result;
    }

    private static IEnumerable<string> GetMissingKeys(Dictionary<string, string> referenceEntries, Dictionary<string, string> entries, string language)
    {
        var pluralGroups = new HashSet<(string, bool)>();

        foreach (var key in referenceEntries.Keys)
        {
            if (!PluralForms.TryParse(key, referenceEntries.ContainsKey, out var pluralKey))
            {
                if (!entries.ContainsKey(key))
                    yield return key;

                continue;
            }

            if (!pluralGroups.Add((pluralKey.BaseKey, pluralKey.Ordinal)))
                continue;

            foreach (var category in PluralForms.GetCategories(language, pluralKey.Ordinal))
            {
                if (!entries.ContainsKey(pluralKey.GetKey(category)))
                    yield return pluralKey.GetKey(category);
            }
        }
    }

    private static IEnumerable<string> GetPlaceholderMismatches(Dictionary<string, string> referenceEntries, Dictionary<string, string> entries)
    {
        foreach (var entry in entries)
        {
            var isPlural = PluralForms.TryParse(entry.Key, k => entries.ContainsKey(k) || referenceEntries.ContainsKey(k), out var pluralKey);
            var referenceValue = referenceEntries.TryGetValue(entry.Key, out var value) ? value
                : isPlural && referenceEntries.TryGetValue(pluralKey.GetKey(PluralForms.Other), out value) ? value
                : null;

            if (string.IsNullOrEmpty(referenceValue) || entry.Value.Length == 0)
                continue;

            var expected = Placeholders.Get(referenceValue);
            var actual = Placeholders.Get(entry.Value);

            if (isPlural)
            {
                expected.Remove("{{count}}");
                actual.Remove("{{count}}");
            }

            var missing = expected.Except(actual).ToList();
            var unexpected = actual.Except(expected).ToList();

            if (missing.Count == 0 && unexpected.Count == 0)
                continue;

            var details = new List<string>();

            if (missing.Count > 0)
                details.Add("missing " + string.Join(", ", missing));

            if (unexpected.Count > 0)
                details.Add("unexpected " + string.Join(", ", unexpected));

            yield return $"{entry.Key}  {string.Join("; ", details)}";
        }
    }

    private static void CheckUsage(TranslationFiles files, string referenceLanguage, Dictionary<string, Dictionary<string, Dictionary<string, string>>> resources,
        KeyExtractor extractor, string[] sourcePaths, CheckReport report, string namespaceSeparator)
    {
        var keys = extractor.Extract(sourcePaths);
        var usage = new KeyUsage(files.KeySeparator, namespaceSeparator);
        usage.AddRange(keys);
        usage.AddGeneratedMemberChains(extractor.GeneratedMemberChains);

        foreach (var namespaces in resources.Values)
        {
            foreach (var entries in namespaces)
            {
                foreach (var value in entries.Value.Values)
                    usage.AddNestings(entries.Key, value);
            }
        }

        foreach (var entries in resources[referenceLanguage])
        {
            foreach (var key in entries.Value.Keys.Where(k => !usage.IsUsed(entries.Key, k)))
                report.Unused.Add($"{referenceLanguage}  {entries.Key}:{key}");
        }

        foreach (var key in keys.Where(k => !k.ReturnsObject && files.HasNamespace(k.Namespace)))
        {
            var entries = resources[referenceLanguage].TryGetValue(key.Namespace, out var namespaceEntries) ? namespaceEntries : [];

            foreach (var missingKey in key.GetKeys(referenceLanguage).Where(k => !entries.ContainsKey(k)))
            {
                var problem = $"{referenceLanguage}  {key.Namespace}:{missingKey}";

                if (!report.Missing.Contains(problem))
                    report.Missing.Add(problem);
            }
        }
    }

    private sealed class CheckReport
    {
        public List<string> Missing { get; } = [];

        public List<string> Empty { get; } = [];

        public List<string> Mismatches { get; } = [];

        public List<string> Unused { get; } = [];

        public int Write(TextWriter output)
        {
            WriteSection(output, "Missing translations", Missing);
            WriteSection(output, "Empty values", Empty);
            WriteSection(output, "Placeholder mismatches", Mismatches);
            WriteSection(output, "Unused keys", Unused);

            var count = Missing.Count + Empty.Count + Mismatches.Count + Unused.Count;

            if (count == 0)
            {
                output.WriteLine("No problems found.");

                return ExitCodes.Success;
            }

            output.WriteLine($"{count} problems found.");

            return ExitCodes.ProblemsFound;
        }

        private static void WriteSection(TextWriter output, string title, List<string> problems)
        {
            if (problems.Count == 0)
                return;

            output.WriteLine($"{title} ({problems.Count}):");

            foreach (var problem in problems)
                output.WriteLine("  " + problem);
        }
    }
}
