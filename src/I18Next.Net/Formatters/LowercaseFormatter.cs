using System;
using System.Globalization;

using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

/// <summary>
///     Converts values to lowercase with the casing rules of the language for the <c>lowercase</c> format, e.g.
///     <c>{{name, lowercase}}</c>.
/// </summary>
public class LowercaseFormatter : IFormatter
{
    /// <inheritdoc />
    public bool CanFormat(object value, string format, string language)
    {
        return string.Equals(format, "lowercase", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    /// <exception cref="System.Globalization.CultureNotFoundException">The language is no known culture.</exception>
    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var cultureInfo = CultureInfo.GetCultureInfo(language);

        return value.ToString().ToLower(cultureInfo);
    }
}
