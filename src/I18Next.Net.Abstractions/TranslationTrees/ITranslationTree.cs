using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     The translations of one namespace in one language, as loaded by an
///     <see cref="I18Next.Net.Backends.ITranslationBackend" />. Keys are paths whose nested parts are separated by dots,
///     e.g. <c>menu.file.open</c>, and array items use their index as part, e.g. <c>list.0</c>.
/// </summary>
public interface ITranslationTree
{
    /// <summary>
    ///     The namespace the translations belong to.
    /// </summary>
    string Namespace { get; set; }

    /// <summary>
    ///     Gets all translations of the tree.
    /// </summary>
    /// <returns>A new dictionary with the translations keyed by their full dotted key.</returns>
    IDictionary<string, string> GetAllValues();

    /// <summary>
    ///     Gets the translation of the given key without any interpolation, plural or context handling. The translator
    ///     appends plural and context suffixes itself and asks for the suffixed keys.
    /// </summary>
    /// <param name="key">The full dotted key, without namespace.</param>
    /// <param name="args">
    ///     The arguments of the translation request, may be <c>null</c>. The built-in trees ignore them.
    /// </param>
    /// <returns>
    ///     The translation or <c>null</c> if the key does not exist. Hierarchical trees may throw a
    ///     <c>TranslationKeyInvalidException</c> instead when the key leads to a group of translations or goes below a
    ///     translation.
    /// </returns>
    string GetValue(string key, IDictionary<string, object> args);
}
