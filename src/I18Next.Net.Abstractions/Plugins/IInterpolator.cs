using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins;

/// <summary>
///     Translates a key nested into a translation, passed to <see cref="IInterpolator.NestAsync" /> by the translator.
/// </summary>
/// <param name="language">The language to translate into.</param>
/// <param name="key">The nested key, optionally with a namespace prefix.</param>
/// <param name="args">The arguments of the nested translation, may be <c>null</c>.</param>
/// <returns>The translation of the nested key.</returns>
public delegate Task<string> TranslateAsyncDelegate(string language, string key, IDictionary<string, object> args);

/// <summary>
///     An interpolator plugin which inserts the arguments into translations (e.g. <c>{{name}}</c> in i18next format) and
///     resolves nested translations (e.g. <c>$t(key)</c>). The translator calls it for each resolved translation unless
///     interpolation or nesting are disabled.
/// </summary>
public interface IInterpolator
{
    /// <summary>
    ///     The formatter plugins used to format interpolated values, e.g. <c>{{price, currency}}</c>. Interpolators
    ///     without formatter support may throw a <see cref="System.NotSupportedException" />.
    /// </summary>
    List<IFormatter> Formatters { get; }

    /// <summary>
    ///     Checks whether a translation contains nested translations. The translator only calls
    ///     <see cref="NestAsync" /> when this returns <c>true</c>, so interpolators without nesting support return
    ///     <c>false</c>.
    /// </summary>
    /// <param name="source">The interpolated translation.</param>
    /// <returns>Whether the translation may contain nested translations.</returns>
    bool CanNest(string source);

    /// <summary>
    ///     Inserts the arguments into the placeholders of a translation.
    /// </summary>
    /// <param name="source">The translation containing the placeholders.</param>
    /// <param name="key">
    ///     The key of the translation, may be <c>null</c>, e.g. for the arguments of nested translations.
    /// </param>
    /// <param name="language">The language of the translation, e.g. used to format values.</param>
    /// <param name="args">
    ///     The arguments to insert, may be <c>null</c>. The translator passes the <c>replace</c> argument instead when
    ///     it is set.
    /// </param>
    /// <returns>The translation with the inserted arguments.</returns>
    Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args);

    /// <summary>
    ///     Replaces the nested translations of a translation with their translations, like
    ///     <c>$t(key)</c> or <c>$t(key, { "count": 2 })</c> in i18next format.
    /// </summary>
    /// <param name="source">The interpolated translation containing the nested translations.</param>
    /// <param name="language">The language of the translation.</param>
    /// <param name="args">The arguments of the translation, may be <c>null</c>.</param>
    /// <param name="translateAsync">Translates the nested keys with the options of the current translation.</param>
    /// <returns>The translation with the nested translations replaced.</returns>
    Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync);
}
