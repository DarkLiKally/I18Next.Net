using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using I18Next.Net.Plugins;

namespace I18Next.Net.Formatters;

/// <summary>
///     Provides the built-in i18next formats number, currency and datetime, e.g. <c>{{value, number(minimumFractionDigits: 2)}}</c>,
///     <c>{{value, currency(USD)}}</c> or <c>{{value, datetime(dateStyle: long; timeStyle: short)}}</c>.
/// </summary>
public class IntlFormatter : IFormatter
{
    private static readonly ConcurrentDictionary<(string Currency, string Language), (string Symbol, int DecimalDigits)> Currencies = new();

    public bool CanFormat(object value, string format, string language)
    {
        if (value == null || format == null)
            return false;

        switch (GetFormatName(format))
        {
            case "number":
            case "currency":
                return IsNumber(value);
            case "datetime":
                return value is DateTime || value is DateTimeOffset;
            default:
                return false;
        }
    }

    public string Format(object value, string format, string language)
    {
        if (value == null)
            return null;

        var options = ParseOptions(format, out var positionalOption);
        var culture = GetCulture(language);

        switch (GetFormatName(format))
        {
            case "number":
                return FormatNumber((IFormattable) value, options, culture);
            case "currency":
                return FormatCurrency((IFormattable) value, options, positionalOption, culture);
            case "datetime":
                return FormatDateTime((IFormattable) value, options, culture);
            default:
                return value.ToString();
        }
    }

    private static string FormatNumber(IFormattable value, IDictionary<string, string> options, CultureInfo culture)
    {
        var minimumFractionDigits = GetIntOption(options, "minimumFractionDigits", 0);
        var maximumFractionDigits = Math.Max(GetIntOption(options, "maximumFractionDigits", Math.Max(3, minimumFractionDigits)), minimumFractionDigits);
        var useGrouping = !options.TryGetValue("useGrouping", out var grouping) || !string.Equals(grouping, "false", StringComparison.OrdinalIgnoreCase);

        var format = useGrouping ? "#,##0" : "0";

        if (maximumFractionDigits > 0)
            format += "." + new string('0', minimumFractionDigits) + new string('#', maximumFractionDigits - minimumFractionDigits);

        return RoundAwayFromZero(value, maximumFractionDigits).ToString(format, culture);
    }

    private static string FormatCurrency(IFormattable value, IDictionary<string, string> options, string positionalOption, CultureInfo culture)
    {
        if (!options.TryGetValue("currency", out var currency))
            currency = positionalOption;

        var numberFormat = (NumberFormatInfo) culture.NumberFormat.Clone();
        var defaultFractionDigits = numberFormat.CurrencyDecimalDigits;

        if (!string.IsNullOrEmpty(currency))
        {
            var currencyInfo = GetCurrency(currency.ToUpperInvariant(), culture);

            numberFormat.CurrencySymbol = currencyInfo.Symbol;
            defaultFractionDigits = currencyInfo.DecimalDigits;
        }

        var fractionDigits = GetIntOption(options, "maximumFractionDigits", GetIntOption(options, "minimumFractionDigits", defaultFractionDigits));

        return RoundAwayFromZero(value, fractionDigits).ToString("C" + fractionDigits.ToString(CultureInfo.InvariantCulture), numberFormat);
    }

    private static string FormatDateTime(IFormattable value, IDictionary<string, string> options, CultureInfo culture)
    {
        options.TryGetValue("dateStyle", out var dateStyle);
        options.TryGetValue("timeStyle", out var timeStyle);

        if (dateStyle == null && timeStyle == null)
            dateStyle = "short";

        var formats = new List<string>(2);

        if (dateStyle != null)
            formats.Add(dateStyle == "full" || dateStyle == "long" ? culture.DateTimeFormat.LongDatePattern : culture.DateTimeFormat.ShortDatePattern);

        if (timeStyle != null)
            formats.Add(timeStyle == "short" ? culture.DateTimeFormat.ShortTimePattern : culture.DateTimeFormat.LongTimePattern);

        return value.ToString(string.Join(" ", formats), culture);
    }

    private static IFormattable RoundAwayFromZero(IFormattable value, int fractionDigits)
    {
        switch (value)
        {
            case double doubleValue when !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue):
                return Math.Round(doubleValue, Math.Min(fractionDigits, 15), MidpointRounding.AwayFromZero);
            case float floatValue when !float.IsNaN(floatValue) && !float.IsInfinity(floatValue):
                return Math.Round((double) floatValue, Math.Min(fractionDigits, 15), MidpointRounding.AwayFromZero);
            case decimal decimalValue:
                return Math.Round(decimalValue, Math.Min(fractionDigits, 28), MidpointRounding.AwayFromZero);
            default:
                return value;
        }
    }

    private static (string Symbol, int DecimalDigits) GetCurrency(string currency, CultureInfo culture)
    {
        return Currencies.GetOrAdd((currency, culture.Name), key =>
        {
            if (TryGetRegion(culture, out var cultureRegion) && cultureRegion.ISOCurrencySymbol == key.Currency)
                return (culture.NumberFormat.CurrencySymbol, culture.NumberFormat.CurrencyDecimalDigits);

            var candidates = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c => TryGetRegion(c, out var region) ? (Culture: c, Region: region) : (Culture: c, Region: null))
                .Where(c => c.Region != null && c.Region.ISOCurrencySymbol == key.Currency)
                .ToList();

            var match = candidates.FirstOrDefault(c => c.Culture.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);

            if (match.Region == null)
                match = candidates.FirstOrDefault(c => c.Culture.TwoLetterISOLanguageName == "en");

            if (match.Region == null)
                match = candidates.FirstOrDefault();

            if (match.Region == null)
                return (key.Currency, 2);

            return (match.Region.CurrencySymbol, match.Culture.NumberFormat.CurrencyDecimalDigits);
        });
    }

    private static bool TryGetRegion(CultureInfo culture, out RegionInfo region)
    {
        try
        {
            region = new RegionInfo(culture.Name);
            return true;
        }
        catch (ArgumentException)
        {
            region = null;
            return false;
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

    private static string GetFormatName(string format)
    {
        var optionsIndex = format.IndexOf('(');
        var name = optionsIndex > -1 ? format.Substring(0, optionsIndex) : format;

        return name.Trim().ToLowerInvariant();
    }

    private static int GetIntOption(IDictionary<string, string> options, string name, int defaultValue)
    {
        if (options.TryGetValue(name, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return Math.Max(0, Math.Min(result, 20));

        return defaultValue;
    }

    private static bool IsNumber(object value)
    {
        return value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long ||
               value is ulong || value is float || value is double || value is decimal;
    }

    private static IDictionary<string, string> ParseOptions(string format, out string positionalOption)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        positionalOption = null;

        var start = format.IndexOf('(');
        var end = format.LastIndexOf(')');

        if (start < 0 || end <= start)
            return options;

        foreach (var option in format.Substring(start + 1, end - start - 1).Split(';'))
        {
            var separatorIndex = option.IndexOf(':');

            if (separatorIndex < 0)
            {
                if (!string.IsNullOrWhiteSpace(option))
                    positionalOption = option.Trim();

                continue;
            }

            var name = option.Substring(0, separatorIndex).Trim();
            var value = option.Substring(separatorIndex + 1).Trim().Trim('\'', '"');

            options[name] = value;
        }

        return options;
    }
}
