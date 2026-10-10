using System;
using System.Collections.Generic;

using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Extraction;

/// <summary>
///     Decides whether a translation key is used by the extracted keys or by nestings in translations. Plural and context
///     variants and the children of a used key count as used.
/// </summary>
internal sealed class KeyUsage(string keySeparator, string namespaceSeparator)
{
    private readonly HashSet<(string Namespace, string Key)> _keys = [];

    public void Add(ExtractedKey key)
    {
        _keys.Add((key.Namespace, key.Key));
    }

    public void AddRange(IEnumerable<ExtractedKey> keys)
    {
        foreach (var key in keys)
            Add(key);
    }

    /// <summary>
    ///     Marks the keys nested with <c>$t(key)</c> in a translation as used.
    /// </summary>
    public void AddNestings(string @namespace, string text)
    {
        foreach (var nestedKey in Placeholders.GetNestedKeys(text))
        {
            var separatorIndex = string.IsNullOrEmpty(namespaceSeparator) ? -1 : nestedKey.IndexOf(namespaceSeparator, StringComparison.Ordinal);

            _keys.Add(separatorIndex > 0
                ? (nestedKey.Substring(0, separatorIndex), nestedKey.Substring(separatorIndex + namespaceSeparator.Length))
                : (@namespace, nestedKey));
        }
    }

    public bool IsUsed(string @namespace, string key)
    {
        if (_keys.Contains((@namespace, key)))
            return true;

        for (var i = key.Length - 1; i > 0; i--)
        {
            var isVariant = string.CompareOrdinal(key, i, PluralForms.Separator, 0, PluralForms.Separator.Length) == 0;
            var isChild = keySeparator.Length > 0 && string.CompareOrdinal(key, i, keySeparator, 0, keySeparator.Length) == 0;

            if ((isVariant || isChild) && _keys.Contains((@namespace, key.Substring(0, i))))
                return true;
        }

        return false;
    }
}
