using System.Collections.Generic;

namespace Localizer.Plugins;

/// <summary>
///     Abstraction for a post processor plugin for I18Next.
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    ///     Keyword to identify the post processor.
    /// </summary>
    string Keyword { get; }

    /// <summary>
    ///     Apply the post processing onto the given result string before interpolation and nesting..
    /// </summary>
    /// <param name="key">The original translation key.</param>
    /// <param name="value">The translated value.</param>
    /// <param name="args">Arguments passed into the interpolation.</param>
    /// <param name="language">The target language.</param>
    /// <param name="translator">The translator used for translation. Can be used for further translating things.</param>
    /// <returns>Post processed value.</returns>
    string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
#if NET6_0_OR_GREATER
    {
        return value;
    }
#else
    ;
#endif

    /// <summary>
    ///     Apply the post processing onto the given result string after interpolation and nesting..
    /// </summary>
    /// <param name="key">The original translation key.</param>
    /// <param name="value">The translated value.</param>
    /// <param name="args">Arguments passed into the interpolation.</param>
    /// <param name="language">The target language.</param>
    /// <param name="translator">The translator used for translation. Can be used for further translating things.</param>
    /// <returns>Post processed value.</returns>
    string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
#if NET6_0_OR_GREATER
    {
        return value;
    }
#else
    ;
#endif
}
