using System;
using System.Globalization;

using I18Next.Net.Logging;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

public class DefaultFormatter(ILogger logger) : IFormatter
{
    private static readonly IntlFormatter IntlFormatter = new();

    private readonly ILogger _logger = logger;

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

        if (IntlFormatter.CanFormat(value, format, language))
            return IntlFormatter.Format(value, format, language);

        if (string.Equals(format, "uppercase", StringComparison.OrdinalIgnoreCase))
            return value.ToString().ToUpper(GetCulture(language));

        if (string.Equals(format, "lowercase", StringComparison.OrdinalIgnoreCase))
            return value.ToString().ToLower(GetCulture(language));

        var formatString = $"{{0:{format}}}";

        try
        {
            var cultureInfo = CultureInfo.GetCultureInfo(language);
            return string.Format(cultureInfo, formatString, value);
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogInformation(ex, "Unable to find a culture info for language \"{language}\". Using invariant culture for formatting the value.", language);
            return string.Format(CultureInfo.InvariantCulture, formatString, value);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "The provided format string \"{format}\" is not compatible with the default .NET string formatting functionality. Check your format string or register a custom formatter to handle this format.", format);
            return value.ToString();
        }
    }

    private static CultureInfo GetCulture(string language)
    {
        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
