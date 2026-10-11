namespace I18Next.Net.Plugins;

/// <summary>
///     A formatter plugin which formats interpolated values, e.g. <c>{{price, currency(EUR)}}</c> or
///     <c>{{date, datetime}}</c>. The default interpolator asks its formatters in order and uses the first one which
///     can format the value.
/// </summary>
public interface IFormatter
{
    /// <summary>
    ///     Checks whether the formatter is able to provide formatting for an interpolation.
    /// </summary>
    /// <param name="value">The resolved value for an interpolation to be formatted, may be <c>null</c>.</param>
    /// <param name="format">
    ///     The format provided for the interpolation including its options, e.g. <c>currency(EUR)</c>. <c>null</c> when
    ///     values without format are formatted too.
    /// </param>
    /// <param name="language">The target language.</param>
    /// <returns>Whether the formatter is able to provide formatting.</returns>
    bool CanFormat(object value, string format, string language);

    /// <summary>
    ///     Applies formatting to an interpolation.
    /// </summary>
    /// <param name="value">The resolved value for an interpolation to be formatted, may be <c>null</c>.</param>
    /// <param name="format">
    ///     The format provided for the interpolation including its options, e.g. <c>currency(EUR)</c>. <c>null</c> when
    ///     values without format are formatted too.
    /// </param>
    /// <param name="language">The target language.</param>
    /// <returns>Formatted string value for the interpolation.</returns>
    string Format(object value, string format, string language);
}
