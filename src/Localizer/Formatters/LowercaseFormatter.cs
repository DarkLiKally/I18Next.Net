using System.Globalization;
using Localizer.Plugins;

namespace Localizer.Formatters;

public class LowercaseFormatter : IFormatter
{
    public bool CanFormat(object value, string format, string language)
    {
        return format.ToLowerInvariant() == "lowercase";
    }

    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var cultureInfo = CultureInfo.GetCultureInfo(language);

        return value.ToString().ToLower(cultureInfo);
    }
}