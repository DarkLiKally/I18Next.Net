using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins;

/// <summary>
///     A translator plugin which resolves translations from the backend and applies plurals, contexts, fallbacks,
///     interpolation, nesting and post processing. <see cref="II18Next" /> delegates all translation requests to it.
/// </summary>
public interface ITranslator
{
    /// <summary>
    ///     The post processor plugins. A post processor is applied to a translation when its
    ///     <see cref="IPostProcessor.Keyword" /> is requested with the <c>postProcess</c> argument.
    /// </summary>
    List<IPostProcessor> PostProcessors { get; }

    /// <summary>
    ///     Raised when a key is missing in a namespace of a language the translator looked into, like the i18next
    ///     <c>missingKey</c> event.
    /// </summary>
    event EventHandler<MissingKeyEventArgs> MissingKey;

    /// <summary>
    ///     Translates a key.
    /// </summary>
    /// <param name="language">
    ///     The language to translate into, <c>cimode</c> returns the namespace and key instead.
    /// </param>
    /// <param name="key">
    ///     The key to translate, optionally prefixed with its namespace, e.g. <c>common:buttons.save</c>.
    /// </param>
    /// <param name="args">
    ///     The arguments to insert, may be <c>null</c>. Arguments like <c>count</c>, <c>context</c>, <c>defaultValue</c>,
    ///     <c>replace</c>, <c>keyPrefix</c>, <c>postProcess</c>, <c>interpolate</c> and <c>nest</c> control the
    ///     translation like the i18next options with the same names.
    /// </param>
    /// <param name="options">The default namespace and the fallback languages and namespaces to use.</param>
    /// <returns>
    ///     The translation. If the key is missing in all fallbacks, the default value if given or the key itself. If the
    ///     key leads to a group of translations, an error message unless the group is an array joined with the
    ///     <c>joinArrays</c> argument.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="language" /> or <paramref name="key" /> is <c>null</c> or empty, or
    ///     <paramref name="options" /> is <c>null</c>.
    /// </exception>
    Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options);

    /// <summary>
    ///     Translates all values below a key, like the i18next <c>returnObjects</c> option.
    /// </summary>
    /// <param name="language">The language to translate into.</param>
    /// <param name="key">The key of the group to translate, optionally prefixed with its namespace.</param>
    /// <param name="args">The arguments inserted into all values, may be <c>null</c>.</param>
    /// <param name="options">The default namespace and the fallback languages and namespaces to use.</param>
    /// <returns>
    ///     The translated values keyed by their name with nested groups as dictionaries and arrays as object arrays, or
    ///     <c>null</c> if the key does not lead to a group.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="language" /> or <paramref name="key" /> is <c>null</c> or empty, or
    ///     <paramref name="options" /> is <c>null</c>.
    /// </exception>
    Task<IDictionary<string, object>> TranslateObjectAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options);

    /// <summary>
    ///     Checks whether a translation exists for a key in the language or its fallbacks, without raising
    ///     <see cref="MissingKey" />.
    /// </summary>
    /// <param name="language">The language to check.</param>
    /// <param name="key">The key to check, optionally prefixed with its namespace.</param>
    /// <param name="args">
    ///     The arguments used to resolve the key, e.g. <c>count</c> or <c>context</c>, may be <c>null</c>.
    /// </param>
    /// <param name="options">The default namespace and the fallback languages and namespaces to use.</param>
    /// <returns>Whether a translation exists.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="language" /> or <paramref name="key" /> is <c>null</c> or empty, or
    ///     <paramref name="options" /> is <c>null</c>.
    /// </exception>
    Task<bool> ExistsAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options);
}
