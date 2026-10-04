using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters;

[TestFixture]
public class IntlFormatterFixture
{
    private IntlFormatter _formatter;

    [SetUp]
    public void SetUp()
    {
        _formatter = new IntlFormatter();
    }

    private static string Normalize(string value)
    {
        return value.Replace(' ', ' ').Replace(' ', ' ');
    }

    [TestCase(1234.5, "number", "en-US", ExpectedResult = "1,234.5")]
    [TestCase(1234.5678, "number", "en-US", ExpectedResult = "1,234.568")]
    [TestCase(1234.5, "number(minimumFractionDigits: 2)", "de-DE", ExpectedResult = "1.234,50")]
    [TestCase(1234.5678, "number(maximumFractionDigits: 1; useGrouping: false)", "en-US", ExpectedResult = "1234.6")]
    [TestCase(1234.5678, "number(maximumFractionDigits: 0)", "en-US", ExpectedResult = "1,235")]
    [TestCase(1234, "NUMBER", "en-US", ExpectedResult = "1,234")]
    [TestCase(1234.5, "currency(USD)", "en-US", ExpectedResult = "$1,234.50")]
    [TestCase(1234.5, "currency(EUR)", "de-DE", ExpectedResult = "1.234,50 €")]
    [TestCase(1234.5, "currency(currency: 'USD')", "de-DE", ExpectedResult = "1.234,50 $")]
    [TestCase(1234.5, "currency(usd; minimumFractionDigits: 0)", "en-US", ExpectedResult = "$1,235")]
    [TestCase(1234.5, "currency", "en-US", ExpectedResult = "$1,234.50")]
    [TestCase(1234.5, "currency(XYZ)", "en-US", ExpectedResult = "XYZ1,234.50")]
    [TestCase(1234.5, "currency(EUR)", "de", ExpectedResult = "1.234,50 €")]
    [TestCase(1234.5, "currency(USD)", "en", ExpectedResult = "$1,234.50")]
    [TestCase(1234.5, "currency(JPY)", "ja", ExpectedResult = "￥1,235")]
    [TestCase(1234.5, "number", "invalid culture!", ExpectedResult = "1,234.5")]
    public string Format_Numbers_ShouldFormatLikeIntl(double value, string format, string language)
    {
        _formatter.CanFormat(value, format, language).Should().BeTrue();

        return Normalize(_formatter.Format(value, format, language));
    }

    [Test]
    public void Format_DifferentNumberTypes_ShouldBeSupported()
    {
        foreach (var value in new object[] { (byte) 5, (sbyte) 5, (short) 5, (ushort) 5, 5, 5u, 5L, 5ul, 5f, 5d, 5m })
        {
            _formatter.CanFormat(value, "number", "en").Should().BeTrue();
            _formatter.Format(value, "number(minimumFractionDigits: 1)", "en").Should().Be("5.0");
        }
    }

    [TestCase("datetime", "en-US", ExpectedResult = "1/25/2018")]
    [TestCase("datetime(dateStyle: short)", "de-DE", ExpectedResult = "25.01.18")]
    [TestCase("datetime(dateStyle: long)", "en-US", ExpectedResult = "January 25, 2018")]
    [TestCase("datetime(dateStyle: full)", "en-US", ExpectedResult = "Thursday, January 25, 2018")]
    [TestCase("datetime(dateStyle: long; timeStyle: short)", "de-DE", ExpectedResult = "25. Januar 2018 um 07:37")]
    [TestCase("datetime(timeStyle: medium)", "en-US", ExpectedResult = "7:37:59 AM")]
    public string Format_DateTimes_ShouldUseCultureStyles(string format, string language)
    {
        var value = new DateTime(2018, 1, 25, 7, 37, 59);

        Normalize(_formatter.Format(new DateTimeOffset(value, TimeSpan.Zero), format, language)).Should().NotBeEmpty();

        return Normalize(_formatter.Format(value, format, language));
    }

    [Test]
    public void CanFormat_UnsupportedValuesOrFormats_ShouldReturnFalse()
    {
        _formatter.CanFormat(null, "number", "en").Should().BeFalse();
        _formatter.CanFormat(5, null, "en").Should().BeFalse();
        _formatter.CanFormat("5", "number", "en").Should().BeFalse();
        _formatter.CanFormat(5, "datetime", "en").Should().BeFalse();
        _formatter.CanFormat(DateTime.Now, "number", "en").Should().BeFalse();
        _formatter.CanFormat(5, "N2", "en").Should().BeFalse();
        _formatter.Format(null, "number", "en").Should().BeNull();
        _formatter.Format(5, "unknown", "en").Should().Be("5");
    }

    [Test]
    public async Task Interpolator_IntlFormats_ShouldBeAvailableWithoutRegistration()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        var args = new Dictionary<string, object> { ["price"] = 12.5, ["date"] = new DateTime(2018, 1, 25) };

        var result = await interpolator.InterpolateAsync("{{price, currency(EUR)}} on {{date, datetime}} or {{price, N1}}", "key", "en-US", args);

        result.Should().Be("€12.50 on 1/25/2018 or 12.5");
    }
}
