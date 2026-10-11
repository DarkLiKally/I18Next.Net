using System.Collections.Generic;

namespace I18Next.Net.Plugins;

/// <summary>
///     A post processor plugin which transforms translations requested with its keyword in the <c>postProcess</c>
///     argument, like the i18next post processors, e.g. <c>new { postProcess = "sprintf" }</c>.
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    ///     The name identifying the post processor in the <c>postProcess</c> argument, which takes a single name, a comma
    ///     separated list or a string array. Compared case-sensitively.
    /// </summary>
    string Keyword { get; }

    /// <summary>
    ///     Applies the post processing to a translation before interpolation and nesting, e.g. to add placeholders or
    ///     nestings. Return <paramref name="value" /> unchanged to only process the result. On .NET 6 and later the default
    ///     implementation returns the value unchanged.
    /// </summary>
    /// <param name="key">The key of the translation, without namespace.</param>
    /// <param name="value">The translation to process.</param>
    /// <param name="args">The arguments of the translation, may be <c>null</c>.</param>
    /// <param name="language">The target language.</param>
    /// <param name="translator">The calling translator, e.g. to translate further keys.</param>
    /// <returns>The post processed translation.</returns>
    string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
#if NET6_0_OR_GREATER
    {
        return value;
    }
#else
    ;
#endif

    /// <summary>
    ///     Applies the post processing to a translation after interpolation and nesting. On .NET 6 and later the default
    ///     implementation returns the value unchanged.
    /// </summary>
    /// <param name="key">The key of the translation, without namespace.</param>
    /// <param name="value">The translation to process.</param>
    /// <param name="args">The arguments of the translation, may be <c>null</c>.</param>
    /// <param name="language">The target language.</param>
    /// <param name="translator">The calling translator, e.g. to translate further keys.</param>
    /// <returns>The post processed translation.</returns>
    string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
#if NET6_0_OR_GREATER
    {
        return value;
    }
#else
    ;
#endif
}
