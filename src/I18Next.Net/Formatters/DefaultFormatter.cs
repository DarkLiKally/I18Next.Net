using System.Globalization;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters
{
    public class DefaultFormatter : IFormatter
    {
        public bool CanFormat(object value, string format, string language)
        {
            return true;
        }

        public string Format(object value, string format, string language)
        {
            if (value == null)
                return null;

            if (format == null)
                return value.ToString();

            var cultureInfo = CultureInfo.GetCultureInfo(language);

            return string.Format(cultureInfo, "{0:" + format + "}", value);
        }
    }
}
