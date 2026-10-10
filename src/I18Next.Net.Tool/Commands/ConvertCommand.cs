using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.Tool.Conversion;
using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

internal static class ConvertCommand
{
    private static readonly string[] Formats = ["json", "yaml", "resx"];

    public static Command Create()
    {
        var input = new Argument<string>("input") { Description = "The file or directory to convert." };
        var output = new Argument<string>("output") { Description = "The file or directory to write." };
        var from = new Option<string>("--from") { Description = "The input format: json, yaml or resx. Defaults to the file extension." };
        var to = new Option<string>("--to") { Description = "The output format: json, yaml or resx. Defaults to the file extension." };
        from.AcceptOnlyFromAmong(Formats);
        to.AcceptOnlyFromAmong(Formats);
        var flat = new Option<bool>("--flat") { Description = "Writes flat keys like a.b instead of nested objects." };
        var nested = new Option<bool>("--nested") { Description = "Nests flat keys like a.b at the key separator." };
        var keySeparator = new LocalesOptions().KeySeparator;

        var command = new Command("convert",
            "Converts translation files between i18next JSON, YAML and .resx, and between flat and nested keys. Directories are converted file by file.")
        {
            input,
            output,
            from,
            to,
            flat,
            nested,
            keySeparator
        };

        command.SetToolAction((parseResult, writer, _) =>
        {
            if (parseResult.GetValue(flat) && parseResult.GetValue(nested))
                throw new ToolException("--flat and --nested cannot be combined.");

            var settings = new ConvertSettings(parseResult.GetValue(from), parseResult.GetValue(to), parseResult.GetValue(flat), parseResult.GetValue(nested),
                LocalesOptions.GetKeySeparator(parseResult.GetValue(keySeparator)));
            var inputPath = parseResult.GetValue(input);
            var outputPath = parseResult.GetValue(output);

            if (Directory.Exists(inputPath))
                return Task.FromResult(ConvertDirectory(inputPath, outputPath, settings, writer));

            if (!File.Exists(inputPath))
                throw new ToolException($"The input {inputPath} does not exist.");

            ConvertFile(inputPath, outputPath, settings.From ?? GetFormat(inputPath), settings.To ?? GetFormat(outputPath), settings);
            writer.WriteLine($"Converted {inputPath} to {outputPath}.");

            return Task.FromResult(ExitCodes.Success);
        });

        return command;
    }

    private static int ConvertDirectory(string inputPath, string outputPath, ConvertSettings settings, TextWriter writer)
    {
        if (settings.To == null)
            throw new ToolException("--to is required to convert a directory.");

        var files = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories)
            .Where(f => GetFormat(f, false) is { } format && (settings.From == null ? format != settings.To : format == settings.From))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            var relativePath = file.Substring(inputPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var target = Path.Combine(outputPath, Path.ChangeExtension(relativePath, "." + settings.To));

            ConvertFile(file, target, GetFormat(file), settings.To, settings);
        }

        writer.WriteLine($"Converted {files.Count} files from {inputPath} to {outputPath}.");

        return ExitCodes.Success;
    }

    private static void ConvertFile(string inputPath, string outputPath, string from, string to, ConvertSettings settings)
    {
        var text = File.ReadAllText(inputPath);
        TranslationDocument document;

        try
        {
            document = from switch
            {
                "yaml" => new TranslationDocument(YamlConverter.Read(text), settings.KeySeparator),
                "resx" => TranslationDocument.FromEntries(ResxConverter.Read(text), settings.KeySeparator),
                _ => TranslationDocument.Parse(text.TrimStart('﻿'), settings.KeySeparator)
            };
        }
        catch (FormatException e)
        {
            throw new ToolException($"{inputPath} is not a valid {from} file: {e.Message}", e);
        }

        if (settings.Flat)
            document = TranslationDocument.FromEntries(document.GetEntries(), string.Empty);
        else if (settings.Nested)
            document = TranslationDocument.FromEntries(document.GetEntries(), settings.KeySeparator);

        var result = to switch
        {
            "yaml" => YamlConverter.Write(document.Root),
            "resx" => ResxConverter.Write(document.GetEntries()),
            _ => document.ToJson()
        };

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));

        if (directory != null)
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, result, new UTF8Encoding(false));
    }

    private static string GetFormat(string path, bool required = true)
    {
        var format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            ".resx" => "resx",
            _ => null
        };

        if (format == null && required)
            throw new ToolException($"The format of {path} cannot be detected from its extension, pass --from or --to.");

        return format;
    }

    private sealed class ConvertSettings(string from, string to, bool flat, bool nested, string keySeparator)
    {
        public string From { get; } = from;

        public string To { get; } = to;

        public bool Flat { get; } = flat;

        public bool Nested { get; } = nested;

        public string KeySeparator { get; } = keySeparator;
    }
}
