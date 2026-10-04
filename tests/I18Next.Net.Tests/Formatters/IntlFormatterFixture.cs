using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Formatters;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class IntlFormatterFixture
{
    public IntlFormatterFixture()
    {
        _formatter = new IntlFormatter();
    }
    private readonly IntlFormatter _formatter;


    private static string Normalize(string value)
    {
        return value.Replace(' ', ' ').Replace(' ', ' ');
    }

    [Theory]
    [InlineData(1234.5, "number", "en-US", "1,234.5")]
    [InlineData(1234.5678, "number", "en-US", "1,234.568")]
    [InlineData(1234.5, "number(minimumFractionDigits: 2)", "de-DE", "1.234,50")]
    [InlineData(1234.5678, "number(maximumFractionDigits: 1; useGrouping: false)", "en-US", "1234.6")]
    [InlineData(1234.5678, "number(maximumFractionDigits: 0)", "en-US", "1,235")]
    [InlineData(1234, "NUMBER", "en-US", "1,234")]
    [InlineData(1234.5, "currency(USD)", "en-US", "$1,234.50")]
    [InlineData(1234.5, "currency(EUR)", "de-DE", "1.234,50 €")]
    [InlineData(1234.5, "currency(currency: 'USD')", "de-DE", "1.234,50 $")]
    [InlineData(1234.5, "currency(usd; minimumFractionDigits: 0)", "en-US", "$1,235")]
    [InlineData(1234.5, "currency", "en-US", "$1,234.50")]
    [InlineData(1234.5, "currency(XYZ)", "en-US", "XYZ1,234.50")]
    [InlineData(1234.5, "currency(EUR)", "de", "1.234,50 €")]
    [InlineData(1234.5, "currency(USD)", "en", "$1,234.50")]
    [InlineData(1234.5, "currency(JPY)", "ja", "￥1,235")]
    [InlineData(1234.5, "number", "invalid culture!", "1,234.5")]
    public void Format_Numbers_ShouldFormatLikeIntl(double value, string format, string language, string expected)
    {
        _formatter.CanFormat(value, format, language).ShouldBeTrue();

#if NETFRAMEWORK
        // The Windows culture data of .NET Framework uses the half-width yen sign.
        expected = expected.Replace('\uFFE5', '\u00A5');
#endif

        Normalize(_formatter.Format(value, format, language)).ShouldBe(expected);
    }

    [Fact]
    public void Format_DifferentNumberTypes_ShouldBeSupported()
    {
        foreach (var value in new object[] { (byte)5, (sbyte)5, (short)5, (ushort)5, 5, 5u, 5L, 5ul, 5f, 5d, 5m })
        {
            _formatter.CanFormat(value, "number", "en").ShouldBeTrue();
            _formatter.Format(value, "number(minimumFractionDigits: 1)", "en").ShouldBe("5.0");
        }
    }

    [Theory]
    [InlineData("datetime", "en-US", "1/25/2018")]
    [InlineData("datetime(dateStyle: short)", "de-DE", "25.01.18")]
    [InlineData("datetime(dateStyle: long)", "en-US", "January 25, 2018")]
    [InlineData("datetime(dateStyle: full)", "en-US", "Thursday, January 25, 2018")]
    [InlineData("datetime(dateStyle: long; timeStyle: short)", "de-DE", "25. Januar 2018 um 07:37")]
    [InlineData("datetime(timeStyle: medium)", "en-US", "7:37:59 AM")]
    public void Format_DateTimes_ShouldUseCultureStyles(string format, string language, string expected)
    {
        var value = new DateTime(2018, 1, 25, 7, 37, 59);

        Normalize(_formatter.Format(new DateTimeOffset(value, TimeSpan.Zero), format, language)).ShouldNotBeEmpty();

        Normalize(_formatter.Format(value, format, language)).ShouldBe(expected);
    }

    [Fact]
    public void CanFormat_UnsupportedValuesOrFormats_ShouldReturnFalse()
    {
        _formatter.CanFormat(null, "number", "en").ShouldBeFalse();
        _formatter.CanFormat(5, null, "en").ShouldBeFalse();
        _formatter.CanFormat("5", "number", "en").ShouldBeFalse();
        _formatter.CanFormat(5, "datetime", "en").ShouldBeFalse();
        _formatter.CanFormat(DateTime.Now, "number", "en").ShouldBeFalse();
        _formatter.CanFormat(5, "N2", "en").ShouldBeFalse();
        _formatter.Format(null, "number", "en").ShouldBeNull();
        _formatter.Format(5, "unknown", "en").ShouldBe("5");
    }

    [Fact]
    public async Task Interpolator_IntlFormats_ShouldBeAvailableWithoutRegistration()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        var args = new Dictionary<string, object> { ["price"] = 12.5, ["date"] = new DateTime(2018, 1, 25) };

        var result = await interpolator.InterpolateAsync("{{price, currency(EUR)}} on {{date, datetime}} or {{price, N1}}", "key", "en-US", args);

        result.ShouldBe("€12.50 on 1/25/2018 or 12.5");
    }
}
