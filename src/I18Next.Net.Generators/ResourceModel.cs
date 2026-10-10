using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace I18Next.Net.Generators;

internal sealed class GroupModel(string name, string fullKey)
{
    public List<GroupModel> Groups { get; } = [];

    public string FullKey { get; } = fullKey;

    public bool IsArray { get; set; }

    public List<KeyModel> Keys { get; } = [];

    public string Name { get; } = name;
}

internal sealed class KeyModel(string name, string fullKey)
{
    public bool Cardinal { get; set; }

    public SortedSet<string> Contexts { get; } = new(System.StringComparer.Ordinal);

    public string Example { get; set; }

    public string FullKey { get; } = fullKey;

    public bool IsArray { get; set; }

    public string Name { get; } = name;

    public bool Ordinal { get; set; }

    public List<string> Placeholders { get; } = [];

    public bool Plural => Cardinal || Ordinal;

    public bool Simple { get; set; }
}

internal sealed class ResourceModel
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{-?\s*([^,}\s]+)[^}]*\}\}", RegexOptions.Compiled);
    private static readonly Regex Version4PluralRegex = new(@"^(.+?)_(ordinal_)?(zero|one|two|few|many|other)$", RegexOptions.Compiled);
    private static readonly Regex LegacyPluralRegex = new(@"^(.+?)_(plural(_\d+)?|\d+)$", RegexOptions.Compiled);

    private ResourceModel(int jsonFormatVersion)
    {
        JsonFormatVersion = jsonFormatVersion;
    }

    public int JsonFormatVersion { get; }

    public static GroupModel BuildNamespace(string @namespace, IReadOnlyList<ResourceEntry> entries, int jsonFormatVersion)
    {
        var model = new ResourceModel(jsonFormatVersion);
        var root = new Node(null);

        foreach (var entry in entries)
        {
            var node = root;

            foreach (var segment in entry.Key.Split('.'))
                node = node.GetOrAdd(segment);

            node.Value = entry.Value;
        }

        var group = new GroupModel(@namespace, "");
        model.Fill(group, root, "");

        return group;
    }

    public static IEnumerable<string> GetPlaceholders(string value)
    {
        foreach (Match match in PlaceholderRegex.Matches(value))
        {
            var name = match.Groups[1].Value;
            var dot = name.IndexOf('.');

            yield return dot > 0 ? name.Substring(0, dot) : name;
        }
    }

    /// <summary>
    ///     Returns the keys of a namespace without plural suffixes, mapped to the placeholders of all their variants.
    /// </summary>
    public static Dictionary<string, HashSet<string>> GetKeyIndex(IReadOnlyList<ResourceEntry> entries, int jsonFormatVersion)
    {
        var index = new Dictionary<string, HashSet<string>>();

        foreach (var entry in entries)
        {
            var key = GetIndexKey(entry.Key, jsonFormatVersion);

            if (!index.TryGetValue(key, out var placeholders))
                index[key] = placeholders = [];

            placeholders.UnionWith(GetPlaceholders(entry.Value));
        }

        return index;
    }

    /// <summary>
    ///     Returns the key without its plural suffix.
    /// </summary>
    public static string GetIndexKey(string key, int jsonFormatVersion)
    {
        var dot = key.LastIndexOf('.');
        var name = dot < 0 ? key : key.Substring(dot + 1);

        return TryStripPluralSuffix(name, jsonFormatVersion, out var stem, out _) ? key.Substring(0, dot + 1) + stem : key;
    }

    /// <summary>
    ///     Returns every key which can be passed to the translation methods: leaves, plural and context base keys and groups.
    /// </summary>
    public static HashSet<string> GetResolvableKeys(IReadOnlyList<ResourceEntry> entries, int jsonFormatVersion)
    {
        var keys = new HashSet<string>(entries.Select(e => e.Key));

        foreach (var key in GetKeyIndex(entries, jsonFormatVersion).Keys)
        {
            keys.Add(key);

            var dot = key.LastIndexOf('.');
            var underscore = key.LastIndexOf('_');

            if (underscore > dot + 1)
                keys.Add(key.Substring(0, underscore));

            for (var i = key.IndexOf('.'); i > 0; i = key.IndexOf('.', i + 1))
                keys.Add(key.Substring(0, i));
        }

        return keys;
    }

    private void Fill(GroupModel group, Node node, string prefix)
    {
        var leaves = node.Children.Where(c => c.Value != null && c.Children.Count == 0).ToList();
        var stems = new Dictionary<Node, (string Stem, bool Plural, bool Ordinal)>();

        foreach (var leaf in leaves)
        {
            var plural = TryStripPluralSuffix(leaf.Name, JsonFormatVersion, out var stem, out var ordinal);
            stems[leaf] = (plural ? stem : leaf.Name, plural, ordinal);
        }

        var stemNames = new HashSet<string>(stems.Values.Select(s => s.Stem));
        var keys = new Dictionary<string, KeyModel>();

        foreach (var leaf in leaves)
        {
            var (stem, plural, ordinal) = stems[leaf];
            var baseName = stem;
            string context = null;
            var underscore = stem.LastIndexOf('_');

            if (underscore > 0 && stemNames.Contains(stem.Substring(0, underscore)))
            {
                baseName = stem.Substring(0, underscore);
                context = stem.Substring(underscore + 1);
            }

            if (!keys.TryGetValue(baseName, out var key))
            {
                keys[baseName] = key = new KeyModel(baseName, prefix + baseName);
                group.Keys.Add(key);
            }

            if (context == null && (!plural || key.Example == null))
                key.Example = leaf.Value;

            if (context != null)
                key.Contexts.Add(context);
            else if (!plural)
                key.Simple = true;

            if (plural && ordinal)
                key.Ordinal = true;
            else if (plural)
                key.Cardinal = true;

            foreach (var placeholder in GetPlaceholders(leaf.Value))
            {
                if (!key.Placeholders.Contains(placeholder))
                    key.Placeholders.Add(placeholder);
            }
        }

        foreach (var child in node.Children.Where(c => c.Children.Count > 0))
        {
            var fullKey = prefix + child.Name;

            if (child.Children.All(c => IsIndex(c.Name)) && child.Children.All(c => c.Children.Count == 0 && c.Value != null))
            {
                var array = new KeyModel(child.Name, fullKey) { IsArray = true, Example = string.Join(", ", child.Children.Select(c => c.Value)) };

                foreach (var placeholder in child.Children.SelectMany(c => GetPlaceholders(c.Value)))
                {
                    if (!array.Placeholders.Contains(placeholder))
                        array.Placeholders.Add(placeholder);
                }

                group.Keys.Add(array);
                continue;
            }

            var childGroup = new GroupModel(child.Name, fullKey) { IsArray = child.Children.All(c => IsIndex(c.Name)) };
            group.Groups.Add(childGroup);
            Fill(childGroup, child, fullKey + ".");
        }
    }

    public static bool TryStripPluralSuffix(string name, int jsonFormatVersion, out string stem, out bool ordinal)
    {
        var match = (jsonFormatVersion >= 4 ? Version4PluralRegex : LegacyPluralRegex).Match(name);

        if (!match.Success)
        {
            stem = name;
            ordinal = false;

            return false;
        }

        stem = match.Groups[1].Value;
        ordinal = jsonFormatVersion >= 4 && match.Groups[2].Success;

        return true;
    }

    private static bool IsIndex(string name)
    {
        return name.Length > 0 && name.All(char.IsDigit);
    }

    private sealed class Node(string name)
    {
        private readonly Dictionary<string, Node> _lookup = [];

        public List<Node> Children { get; } = [];

        public string Name { get; } = name;

        public string Value { get; set; }

        public Node GetOrAdd(string name)
        {
            if (_lookup.TryGetValue(name, out var child))
                return child;

            child = new Node(name);
            _lookup[name] = child;
            Children.Add(child);

            return child;
        }
    }
}
