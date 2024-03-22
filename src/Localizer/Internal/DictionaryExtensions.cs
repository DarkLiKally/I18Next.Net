using System.Collections.Generic;
using System.Linq;

namespace Localizer.Internal;

public static class DictionaryExtensions
{
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