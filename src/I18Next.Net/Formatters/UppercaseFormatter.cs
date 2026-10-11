using System.Globalization;

using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

/// <summary>
///     Converts values to uppercase with the casing rules of the language for the <c>uppercase</c> format, e.g.
///     <c>{{name, uppercase}}</c>.
/// </summary>
public class UppercaseFormatter : IFormatter
{
    /// <inheritdoc />
    public bool CanFormat(object value, string format, string language)
    {
        return format.ToLowerInvariant() == "uppercase";
    }

    /// <inheritdoc />
    /// <exception cref="System.Globalization.CultureNotFoundException">The language is no known culture.</exception>
    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var cultureInfo = CultureInfo.GetCultureInfo(language);

        return value.ToString().ToUpper(cultureInfo);
    }
}
