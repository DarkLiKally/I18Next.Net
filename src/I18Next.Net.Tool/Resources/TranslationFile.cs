using System;
using System.IO;
using System.Text;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     A JSON translation file which is written as UTF-8 without byte order mark, with two spaces indentation and the line
///     endings of the existing file.
/// </summary>
internal sealed class TranslationFile
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    private TranslationFile(string path, string language, string @namespace, TranslationDocument document, string originalText)
    {
        Path = path;
        Language = language;
        Namespace = @namespace;
        Document = document;
        OriginalText = originalText;
    }

    public string Path { get; }

    public string Language { get; }

    public string Namespace { get; }

    public TranslationDocument Document { get; }

    public string OriginalText { get; }

    public bool Exists => OriginalText != null;

    public static TranslationFile Load(string path, string language, string @namespace, string keySeparator)
    {
        if (!File.Exists(path))
            return new TranslationFile(path, language, @namespace, new TranslationDocument(keySeparator), null);

        var text = Utf8WithoutBom.GetString(File.ReadAllBytes(path));

        try
        {
            return new TranslationFile(path, language, @namespace, TranslationDocument.Parse(text.TrimStart('﻿'), keySeparator), text);
        }
        catch (FormatException e)
        {
            throw new ToolException($"{path} is not a valid translation file: {e.Message}", e);
        }
    }

    public string ToText()
    {
        var text = Document.ToJson();

        return OriginalText != null && OriginalText.Contains("\r\n") ? text.Replace("\n", "\r\n") : text;
    }

    /// <summary>
    ///     Writes the file when its content changed.
    /// </summary>
    /// <param name="dryRun">Only checks whether the file would change.</param>
    /// <returns>Whether the content changed.</returns>
    public bool Save(bool dryRun)
    {
        var text = ToText();

        if (text == OriginalText || (!Exists && Document.Root.Count == 0))
            return false;

        if (!dryRun)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)));
            File.WriteAllText(Path, text, Utf8WithoutBom);
        }

        return true;
    }
}
