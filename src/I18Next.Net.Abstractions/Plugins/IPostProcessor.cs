using System.Collections.Generic;

namespace I18Next.Net.Plugins
{
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
        ///     Apply the post processing onto the given value.
        /// </summary>
        /// <param name="key">The original translation key.</param>
        /// <param name="value">The translated value.</param>
        /// <param name="args">Arguments passed into the interpolation.</param>
        /// <returns>Post processed value.</returns>
        string Process(string key, string value, IDictionary<string, object> args);
    }
}
