using System;
using System.Globalization;
using Localizer.Logging;
using Localizer.Plugins;

namespace Localizer.Formatters;

public class DefaultFormatter : IFormatter
{
    private readonly ILogger _logger;

    public DefaultFormatter(ILogger logger)
    {
        _logger = logger;
    }

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

        var formatString = $"{{0:{format}}}";

        try
        {
            var cultureInfo = CultureInfo.GetCultureInfo(language);
            return string.Format(cultureInfo, formatString, value);
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogInformation(ex, "Unable to find a culture info for language \"{language}\". Using invariant culture for formatting the value.");
            return string.Format(CultureInfo.InvariantCulture, formatString, value);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "The provided format string \"{format}\" is not compatible with the default .NET string formatting functionality. Check your format string or register a custom formatter to handle this format.");
            return value.ToString();
        }
    }
}
