using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using I18Next.Net.Plugins;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     Renames the i18next JSON v3 plural keys (<c>key</c> and <c>key_plural</c> or <c>key_0</c> to <c>key_n</c>) to the v4
///     plural keys of the CLDR categories (<c>key_one</c>, <c>key_other</c>). Each v3 suffix gets the category of the first
///     count using it.
/// </summary>
internal sealed class PluralMigration
{
    private const string PluralSuffix = "_plural";

    private readonly string _singularCategory;
    private readonly Dictionary<string, string> _suffixes = new(StringComparer.Ordinal);

    public PluralMigration(string language)
    {
        var resolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version3 };

        for (var count = 0; count <= 200; count++)
        {
            var suffix = resolver.GetPluralSuffix(language, count);

            if (suffix.Length == 0)
                _singularCategory ??= DefaultPluralResolver.GetPluralCategory(language, count);
            else if (!_suffixes.ContainsKey(suffix))
                _suffixes[suffix] = DefaultPluralResolver.GetPluralCategory(language, count);
        }

        if (!_suffixes.ContainsKey(PluralSuffix))
            _singularCategory = null;
    }

    public List<string> Conflicts { get; } = [];

    /// <summary>
    ///     Renames the plural keys in the object and its children.
    /// </summary>
    /// <returns>The number of renamed keys.</returns>
    public int Migrate(JsonObject obj, string path = null)
    {
        var renamed = 0;

        foreach (var child in obj.Where(p => p.Value is JsonObject).ToList())
            renamed += Migrate((JsonObject)child.Value, path == null ? child.Key : path + "." + child.Key);

        var renames = GetRenames(obj, path);

        if (renames.Count == 0)
            return renamed;

        var properties = obj.ToList();
        obj.Clear();

        foreach (var property in properties)
            obj.Add(renames.TryGetValue(property.Key, out var name) ? name : property.Key, property.Value);

        return renamed + renames.Count;
    }

    private Dictionary<string, string> GetRenames(JsonObject obj, string path)
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in obj)
        {
            if (property.Value is not JsonValue)
                continue;

            var name = property.Key;

            if (_singularCategory != null && obj[name + PluralSuffix] is JsonValue)
            {
                renames[name] = name + PluralForms.GetSuffix(_singularCategory, false);
                continue;
            }

            foreach (var suffix in _suffixes)
            {
                if (name.Length <= suffix.Key.Length || !name.EndsWith(suffix.Key, StringComparison.Ordinal))
                    continue;

                var baseKey = name.Substring(0, name.Length - suffix.Key.Length);

                if (suffix.Key == PluralSuffix || obj[baseKey + "_0"] is JsonValue)
                    renames[name] = baseKey + PluralForms.GetSuffix(suffix.Value, false);

                break;
            }
        }

        var conflicts = renames
            .Where(r => (obj.ContainsKey(r.Value) && !renames.ContainsKey(r.Value)) || renames.Count(o => o.Value == r.Value) > 1)
            .ToList();

        foreach (var conflict in conflicts)
        {
            Conflicts.Add($"{(path == null ? conflict.Key : path + "." + conflict.Key)} cannot be renamed to {conflict.Value}, the key exists.");
            renames.Remove(conflict.Key);
        }

        return renames;
    }
}
