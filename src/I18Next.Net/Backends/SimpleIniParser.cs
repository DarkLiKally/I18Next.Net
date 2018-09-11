using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace I18Next.Net.Backends
{
    public class SimpleIniParser
    {
        private readonly Dictionary<string, Dictionary<string, string>> _entries =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

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
            if (!_entries.ContainsKey(section))
                return new string[0];

            return _entries[section].Keys.ToArray();
        }

        public string[] GetSections()
        {
            return _entries.Keys.Where(t => t != "").ToArray();
        }

        public string GetValue(string key)
        {
            return GetValue("", key, null);
        }

        public string GetValue(string section, string key)
        {
            return GetValue(key, section, null);
        }

        public string GetValue(string section, string key, string @default)
        {
            if (!_entries.ContainsKey(section))
                return @default;

            if (!_entries[section].ContainsKey(key))
                return @default;

            return _entries[section][key];
        }

        private void ParseIniContent(string txt)
        {
            var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            _entries[""] = currentSection;

            foreach (var line in txt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
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

                    if (value.StartsWith("\"", StringComparison.Ordinal))
                        currentSection[key] = value.Substring(1, value.Length - 2);
                    else
                        currentSection[key] = value;
                }
            }
        }
    }
}
