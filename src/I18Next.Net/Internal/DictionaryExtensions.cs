using System.Collections.Generic;
using System.Linq;

namespace I18Next.Net.Internal;

/// <summary>
///     Dictionary helpers used by the I18Next.Net packages.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    ///     Merges dictionaries into a new dictionary. Entries of later dictionaries override entries with the same key of
    ///     earlier ones; the given dictionaries are not modified.
    /// </summary>
    /// <param name="me">The first dictionary.</param>
    /// <param name="others">The dictionaries merged over <paramref name="me" /> in order.</param>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <returns>A new dictionary containing the entries of all dictionaries.</returns>
    public static IDictionary<TKey, TValue> MergeLeft<TKey, TValue>(this IDictionary<TKey, TValue> me, params IDictionary<TKey, TValue>[] others)
    {
        var newMap = new Dictionary<TKey, TValue>();

        foreach (var src in new List<IDictionary<TKey, TValue>> { me }.Concat(others))
        {
            foreach (var p in src)
                newMap[p.Key] = p.Value;
        }

        return newMap;
    }
}
