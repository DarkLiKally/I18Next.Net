namespace I18Next.Net.Plugins;

/// <summary>
///     A I18Next formatter plugin used to apply formatting to an interpolation result.
/// </summary>
public interface IFormatter
{
    /// <summary>
    ///     Checks whether the formatter is able to provide formatting for an interpolation.
    /// </summary>
    /// <param name="value">The resolved value for an interpolation to be formatted.</param>
    /// <param name="format">The format string provided for the interpolation.</param>
    /// <param name="language">The target language.</param>
    /// <returns>Whether the formatter is able to provide formatting.</returns>
    bool CanFormat(object value, string format, string language);

    /// <summary>
    ///     Applies formatting to an interpolation.
    /// </summary>
    /// <param name="value">The resolved value for an interpolation to be formatted.</param>
    /// <param name="format">The format string provided for the interpolation.</param>
    /// <param name="language">The target language.</param>
    /// <returns>Formatted string value for the interpolation.</returns>
    string Format(object value, string format, string language);
}