using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace I18Next.Net.Backends;

public class SimpleIniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _entries = new(StringComparer.InvariantCultureIgnoreCase);

    public SimpleIniParser(string iniContent)
    {
        ParseIniContent(iniContent);
    }

    public static SimpleIniParser FromFile(string file)
    {
        var txt = File.ReadAllText(file);

        return new SimpleIniParser(txt);
    }

    public string[] GetKeys(string section)
    {
        return !_entries.ContainsKey(section) ? [] : [.. _entries[section].Keys];
    }

    public string[] GetSections()
    {
        return [.. _entries.Keys.Where(t => t != "")];
    }

    public string GetValue(string key)
    {
        return GetValue("", key, null);
    }

    public string GetValue(string section, string key)
    {
        return GetValue(section, key, null);
    }

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
