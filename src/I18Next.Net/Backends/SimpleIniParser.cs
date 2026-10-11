using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace I18Next.Net.Backends;

/// <summary>
///     A minimal INI parser used by the <see cref="IniFileBackend" />. Supports <c>[section]</c> headers,
///     <c>key=value</c> pairs, values in double quotes and comment lines starting with <c>;</c>. Section and key names
///     are case-insensitive; keys before the first section belong to the section <c>""</c>.
/// </summary>
public class SimpleIniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _entries = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    ///     Parses the given INI content.
    /// </summary>
    /// <param name="iniContent">The content of an INI file.</param>
    public SimpleIniParser(string iniContent)
    {
        ParseIniContent(iniContent);
    }

    /// <summary>
    ///     Parses the given INI file.
    /// </summary>
    /// <param name="file">The path of the INI file.</param>
    /// <returns>The parser containing the parsed entries.</returns>
    public static SimpleIniParser FromFile(string file)
    {
        var txt = File.ReadAllText(file);

        return new SimpleIniParser(txt);
    }

    /// <summary>
    ///     Gets the keys of a section.
    /// </summary>
    /// <param name="section">The section, or <c>""</c> for the keys before the first section.</param>
    /// <returns>The keys of the section, or an empty array if the section does not exist.</returns>
    public string[] GetKeys(string section)
    {
        return !_entries.ContainsKey(section) ? [] : [.. _entries[section].Keys];
    }

    /// <summary>
    ///     Gets the names of all sections, excluding the section <c>""</c> of the keys before the first section.
    /// </summary>
    /// <returns>The names of the sections.</returns>
    public string[] GetSections()
    {
        return [.. _entries.Keys.Where(t => t != "")];
    }

    /// <summary>
    ///     Gets the value of a key before the first section.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value, or null if the key does not exist.</returns>
    public string GetValue(string key)
    {
        return GetValue("", key, null);
    }

    /// <summary>
    ///     Gets the value of a key in a section.
    /// </summary>
    /// <param name="section">The section, or <c>""</c> for the keys before the first section.</param>
    /// <param name="key">The key.</param>
    /// <returns>The value, or null if the section or the key does not exist.</returns>
    public string GetValue(string section, string key)
    {
        return GetValue(section, key, null);
    }

    /// <summary>
    ///     Gets the value of a key in a section.
    /// </summary>
    /// <param name="section">The section, or <c>""</c> for the keys before the first section.</param>
    /// <param name="key">The key.</param>
    /// <param name="default">The value returned if the section or the key does not exist.</param>
    /// <returns>The value, or <paramref name="default" /> if the section or the key does not exist.</returns>
    public string GetValue(string section, string key, string @default)
    {
        if (!_entries.ContainsKey(section))
            return @default;

        return !_entries[section].ContainsKey(key) ? @default : _entries[section][key];
    }

    private void ParseIniContent(string txt)
    {
        var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        _entries[""] = currentSection;

        foreach (var line in txt.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                     .Where(t => !string.IsNullOrWhiteSpace(t))
                     .Select(t => t.Trim()))
        {
            if (line.StartsWith(";", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                var sectionTitle = line.Substring(1, line.LastIndexOf("]", StringComparison.Ordinal) - 1).Trim();
                _entries[sectionTitle] = currentSection;
                continue;
            }

            var idx = line.IndexOf("=", StringComparison.Ordinal);
            if (idx == -1)
            {
                currentSection[line] = "";
            }
            else
            {
                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();

                currentSection[key] = value.StartsWith("\"", StringComparison.Ordinal) ? value.Substring(1, value.Length - 2) : value;
            }
        }
    }
}
