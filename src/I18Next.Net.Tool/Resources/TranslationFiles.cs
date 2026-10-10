using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     The translation files in the i18next layout <c>{path}/{lng}/{ns}.json</c>.
/// </summary>
internal sealed class TranslationFiles(string path, string keySeparator, IReadOnlyList<string> languages = null, IReadOnlyList<string> namespaces = null)
{
    private IReadOnlyList<string> _languages = languages is { Count: > 0 } ? languages : null;
    private IReadOnlyList<string> _namespaces = namespaces is { Count: > 0 } ? namespaces : null;

    public string Path { get; } = path;

    public string KeySeparator { get; } = keySeparator;

    /// <summary>
    ///     The selected languages or all language directories.
    /// </summary>
    public IReadOnlyList<string> Languages => _languages ??= Directory.Exists(Path)
        ? Directory.GetDirectories(Path).Select(System.IO.Path.GetFileName).OrderBy(l => l, StringComparer.Ordinal).ToList()
        : [];

    /// <summary>
    ///     The selected namespaces or the namespaces of all JSON files of the languages.
    /// </summary>
    public IReadOnlyList<string> Namespaces => _namespaces ??= Languages
        .Select(l => System.IO.Path.Combine(Path, l))
        .Where(Directory.Exists)
        .SelectMany(d => Directory.GetFiles(d, "*.json"))
        .Select(System.IO.Path.GetFileNameWithoutExtension)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();

    public bool HasNamespace(string @namespace)
    {
        return _namespaces == null || _namespaces.Contains(@namespace, StringComparer.Ordinal);
    }

    public string GetFilePath(string language, string @namespace)
    {
        return System.IO.Path.Combine(Path, language, @namespace + ".json");
    }

    public TranslationFile Load(string language, string @namespace)
    {
        return TranslationFile.Load(GetFilePath(language, @namespace), language, @namespace, KeySeparator);
    }

    /// <summary>
    ///     Loads the existing files of all selected languages and namespaces.
    /// </summary>
    public IEnumerable<TranslationFile> LoadExisting()
    {
        foreach (var language in Languages)
        {
            foreach (var @namespace in Namespaces)
            {
                if (File.Exists(GetFilePath(language, @namespace)))
                    yield return Load(language, @namespace);
            }
        }
    }

    public void EnsureLanguages()
    {
        if (!Directory.Exists(Path))
            throw new ToolException($"The directory {Path} does not exist.");

        if (Languages.Count == 0)
            throw new ToolException($"No language directories found in {Path}.");
    }
}
