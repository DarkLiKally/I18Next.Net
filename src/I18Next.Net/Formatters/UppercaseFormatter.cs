using System.Globalization;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

public class UppercaseFormatter : IFormatter
{
    public bool CanFormat(object value, string format, string language)
    {
        return format.ToLowerInvariant() == "uppercase";
    }

    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var cultureInfo = CultureInfo.GetCultureInfo(language);

        return value.ToString().ToUpper(cultureInfo);
    }
}