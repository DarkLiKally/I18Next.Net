using System;
using System.Collections.Generic;
using System.Linq;

namespace I18Next.Net.Generators;

internal static class KeySuggestions
{
    private const int MaximumSuggestions = 3;

    /// <summary>
    ///     Returns up to three keys, written like the key of the call, which are the most similar to the unknown key.
    /// </summary>
    public static List<string> Find(ResourceCatalog catalog, TranslationCall call)
    {
        var suggestions = new Dictionary<string, int>();
        var maximum = call.Key.Length <= 4 ? 1 : call.Key.Length <= 8 ? 2 : 3;
        var key = call.Key.ToLowerInvariant();
        var candidates = catalog.GetCandidates(call.Namespace);

        foreach (var candidate in candidates)
        {
            var distance = StringDistance.Get(key, candidate.ToLowerInvariant(), maximum);

            if (distance <= maximum)
                Add(suggestions, call.Prefix + candidate, distance);
        }

        foreach (var stem in GetStems(call.Key, catalog.Target.JsonFormatVersion))
        {
            if (candidates.Contains(stem))
                Add(suggestions, call.Prefix + stem, 1);
        }

        foreach (var @namespace in catalog.Namespaces.Where(n => n != call.Namespace))
        {
            if (catalog.TryGetKeys(@namespace, out var keys) && keys.Contains(call.Key))
                Add(suggestions, @namespace + catalog.Target.NamespaceSeparator + call.Key, 1);
        }

        return suggestions
            .OrderBy(s => s.Value)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .Take(MaximumSuggestions)
            .Select(s => s.Key)
            .ToList();
    }

    private static IEnumerable<string> GetStems(string key, int jsonFormatVersion)
    {
        var dot = key.LastIndexOf('.');
        var name = key.Substring(dot + 1);
        var parent = key.Substring(0, dot + 1);

        foreach (var version in new[] { jsonFormatVersion, 4, 3 })
        {
            if (ResourceModel.TryStripPluralSuffix(name, version, out var stem, out _))
                yield return parent + stem;
        }

        var underscore = name.LastIndexOf('_');

        if (underscore > 0)
            yield return parent + name.Substring(0, underscore);
    }

    private static void Add(Dictionary<string, int> suggestions, string suggestion, int distance)
    {
        if (!suggestions.TryGetValue(suggestion, out var existing) || distance < existing)
            suggestions[suggestion] = distance;
    }
}
