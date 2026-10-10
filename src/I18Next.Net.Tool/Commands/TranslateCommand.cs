using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

using I18Next.Net.MachineTranslation;
using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

internal static class TranslateCommand
{
    public static Command Create(ToolServices services)
    {
        var locales = new LocalesOptions();
        var sourceLanguage = new Option<string>("--source-language")
        {
            Description = "The language the missing translations are translated from.",
            DefaultValueFactory = _ => "en"
        };
        var provider = new Option<string>("--provider")
        {
            Description = "The machine translation service: deepl or azure.",
            DefaultValueFactory = _ => "deepl"
        };
        provider.AcceptOnlyFromAmong("deepl", "azure");
        var authKey = new Option<string>("--auth-key") { Description = "The API key. Defaults to DEEPL_AUTH_KEY or AZURE_TRANSLATOR_KEY." };
        var region = new Option<string>("--region") { Description = "The region of the Azure resource. Defaults to AZURE_TRANSLATOR_REGION." };
        var endpoint = new Option<string>("--endpoint") { Description = "The base address of the API, e.g. for a proxy or a custom Azure endpoint." };
        var dryRun = new Option<bool>("--dry-run", "--check") { Description = "Lists the missing translations without translating them and fails when there are any." };

        var command = new Command("translate", "Fills missing and empty translations of the other languages from the source language with machine translation.");
        locales.AddTo(command);
        command.Options.Add(sourceLanguage);
        command.Options.Add(provider);
        command.Options.Add(authKey);
        command.Options.Add(region);
        command.Options.Add(endpoint);
        command.Options.Add(dryRun);

        command.SetToolAction(async (parseResult, output, cancellationToken) =>
        {
            var files = locales.GetFiles(parseResult);
            files.EnsureLanguages();

            var source = parseResult.GetValue(sourceLanguage);
            var sourceFiles = new TranslationFiles(files.Path, files.KeySeparator, [source]);

            if (!Directory.Exists(Path.Combine(files.Path, source)))
                throw new ToolException($"The source language directory {Path.Combine(files.Path, source)} does not exist.");

            var isDryRun = parseResult.GetValue(dryRun);
            IMachineTranslator translator = null;
            var changed = 0;

            foreach (var @namespace in sourceFiles.Namespaces.Where(files.HasNamespace))
            {
                var sourceEntries = files.Load(source, @namespace).Document.GetEntries().ToList();

                foreach (var language in files.Languages.Where(l => l != source))
                {
                    var file = files.Load(language, @namespace);
                    var missing = GetMissing(sourceEntries, file.Document, language);

                    if (missing.Count == 0)
                        continue;

                    changed++;

                    if (isDryRun)
                    {
                        await output.WriteLineAsync($"{file.Path}: would translate {missing.Count} keys.");

                        foreach (var entry in missing)
                            await output.WriteLineAsync("  " + entry.Key);

                        continue;
                    }

                    translator ??= CreateTranslator(services, parseResult.GetValue(provider), parseResult.GetValue(authKey), parseResult.GetValue(region),
                        parseResult.GetValue(endpoint));

                    var translations = await translator.TranslateAsync(missing.Select(e => e.Value).ToList(), source, language, cancellationToken);

                    for (var i = 0; i < missing.Count; i++)
                    {
                        if (!file.Document.SetValue(missing[i].Key, translations[i]))
                            await output.WriteLineAsync($"{file.Path}: cannot add {missing[i].Key}, a parent key is not an object.");
                    }

                    file.Save(false);
                    await output.WriteLineAsync($"{file.Path}: translated {missing.Count} keys from {source}.");
                }
            }

            if (changed == 0)
                await output.WriteLineAsync("No missing translations.");

            return isDryRun && changed > 0 ? ExitCodes.ProblemsFound : ExitCodes.Success;
        });

        return command;
    }

    /// <summary>
    ///     Gets the keys missing or empty in the document with the source text. Plural keys get the forms of the language,
    ///     translated from the same or the other form of the source language.
    /// </summary>
    private static List<KeyValuePair<string, string>> GetMissing(List<KeyValuePair<string, string>> sourceEntries, TranslationDocument document,
        string language)
    {
        var source = sourceEntries.GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        var pluralGroups = new HashSet<(string, bool)>();
        var result = new List<KeyValuePair<string, string>>();

        foreach (var entry in sourceEntries)
        {
            if (!PluralForms.TryParse(entry.Key, source.ContainsKey, out var pluralKey))
            {
                if (entry.Value.Length > 0 && string.IsNullOrEmpty(document.GetValue(entry.Key)))
                    result.Add(entry);

                continue;
            }

            if (!pluralGroups.Add((pluralKey.BaseKey, pluralKey.Ordinal)))
                continue;

            foreach (var category in PluralForms.GetCategories(language, pluralKey.Ordinal))
            {
                var key = pluralKey.GetKey(category);
                var text = source.TryGetValue(key, out var value) && value.Length > 0 ? value : source[pluralKey.GetKey(PluralForms.Other)];

                if (text.Length > 0 && string.IsNullOrEmpty(document.GetValue(key)))
                    result.Add(new KeyValuePair<string, string>(key, text));
            }
        }

        return result;
    }

    private static IMachineTranslator CreateTranslator(ToolServices services, string provider, string authKey, string region, string endpoint)
    {
        Uri endpointUri = null;

        if (endpoint != null && !Uri.TryCreate(endpoint, UriKind.Absolute, out endpointUri))
            throw new ToolException($"The endpoint {endpoint} is not an absolute URL.");

        if (provider == "azure")
        {
            var key = authKey ?? services.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");

            if (string.IsNullOrWhiteSpace(key))
                throw new ToolException("Pass --auth-key or set the AZURE_TRANSLATOR_KEY environment variable.");

            var azure = new AzureTranslator(services.HttpClient, key, region ?? services.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION"));

            if (endpointUri != null)
                azure.Endpoint = endpointUri;

            return azure;
        }

        var authenticationKey = authKey ?? services.GetEnvironmentVariable("DEEPL_AUTH_KEY");

        if (string.IsNullOrWhiteSpace(authenticationKey))
            throw new ToolException("Pass --auth-key or set the DEEPL_AUTH_KEY environment variable.");

        var deepL = new DeepLTranslator(services.HttpClient, authenticationKey);

        if (endpointUri != null)
            deepL.Endpoint = endpointUri;

        return deepL;
    }
}
