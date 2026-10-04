using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     A translation tree which is able to provide all translations of a group of keys.
/// </summary>
public interface IHierarchicalTranslationTree : ITranslationTree
{
    /// <summary>
    ///     Gets all translations below the given key.
    /// </summary>
    /// <param name="key">The key of the group.</param>
    /// <returns>The translations keyed by their path relative to the group or null if the key does not lead to a group.</returns>
    IDictionary<string, string> GetGroupValues(string key);
}
