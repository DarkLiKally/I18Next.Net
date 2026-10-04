using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class IntlFormatterRelativeTimeAndListFixture
{
    public IntlFormatterRelativeTimeAndListFixture()
    {
        _formatter = new IntlFormatter();
    }
    private IntlFormatter _formatter;


    [Theory]
    [InlineData(3, "relativetime", "en", "in 3 days")]
    [InlineData(-3, "relativetime", "en", "3 days ago")]
    [InlineData(1, "relativetime", "en", "in 1 day")]
    [InlineData(-1, "relativetime(numeric: auto)", "en", "yesterday")]
    [InlineData(0, "relativetime(numeric: auto)", "en", "today")]
    [InlineData(1, "relativetime(numeric: auto)", "en", "tomorrow")]
    [InlineData(5, "relativetime(numeric: auto)", "en", "in 5 days")]
    [InlineData(-1, "relativetime(numeric: always)", "en", "1 day ago")]
    [InlineData(2, "relativetime(quarter)", "en", "in 2 quarters")]
    [InlineData(2, "relativetime(quarters)", "en", "in 2 quarters")]
    [InlineData(-2, "relativetime(range: week; style: short)", "en", "2 wk. ago")]
    [InlineData(5, "relativetime(hour; style: narrow)", "en", "in 5h")]
    [InlineData(10, "relativetime(minute)", "en", "in 10 minutes")]
    [InlineData(-30, "relativetime(second)", "en", "30 seconds ago")]
    [InlineData(-1, "relativetime(year; numeric: auto)", "en", "last year")]
    [InlineData(1, "relativetime(month; numeric: auto)", "en", "next month")]
    [InlineData(-1, "relativetime(numeric: auto)", "de", "gestern")]
    [InlineData(-2, "relativetime(numeric: auto)", "de-AT", "vorgestern")]
    [InlineData(1500, "relativetime(day)", "de-DE", "in 1.500 Tagen")]
    [InlineData(3, "relativetime(month)", "fr", "dans 3 mois")]
    [InlineData(-5, "relativetime(year)", "ru", "5 лет назад")]
    [InlineData(2, "relativetime(day)", "ru", "через 2 дня")]
    [InlineData(21, "relativetime(day)", "ru", "через 21 день")]
    [InlineData(3, "relativetime", "zh-TW", "3 天後")]
    [InlineData(3, "relativetime", "zh", "3天后")]
    [InlineData(3, "relativetime", "unknown", "in 3 days")]
    [InlineData(1.5, "relativetime(hour)", "en", "in 1.5 hours")]
    public void Format_RelativeTime_ShouldMatchIntl(double value, string format, string language, string expected)
    {
        _formatter.CanFormat(value, format, language).ShouldBeTrue();

        _formatter.Format(value, format, language).ShouldBe(expected);
    }

    [Fact]
    public void Format_RelativeTimeNegativeZero_ShouldBePast()
    {
        _formatter.Format(-0.0, "relativetime", "en").ShouldBe("0 days ago");
        _formatter.Format(0, "relativetime", "en").ShouldBe("in 0 days");
    }

    [Fact]
    public void Format_RelativeTimeUnknownUnit_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => _formatter.Format(1, "relativetime(fortnight)", "en"));
    }

    [Theory]
    [InlineData("list", "en", new[] { "a", "b", "c" }, "a, b, and c")]
    [InlineData("list", "en", new[] { "a", "b" }, "a and b")]
    [InlineData("list", "en", new[] { "a", "b", "c", "d" }, "a, b, c, and d")]
    [InlineData("list", "en-GB", new[] { "a", "b", "c" }, "a, b and c")]
    [InlineData("list(type: disjunction)", "en", new[] { "a", "b", "c" }, "a, b, or c")]
    [InlineData("list(type: disjunction)", "de", new[] { "a", "b", "c", "d" }, "a, b, c oder d")]
    [InlineData("list(type: unit)", "en", new[] { "5 hours", "3 minutes" }, "5 hours, 3 minutes")]
    [InlineData("list(type: unit; style: narrow)", "en", new[] { "a", "b", "c" }, "a b c")]
    [InlineData("list(style: short)", "en", new[] { "a", "b", "c" }, "a, b, & c")]
    [InlineData("list", "es", new[] { "a", "b", "c" }, "a, b y c")]
    [InlineData("list", "ja", new[] { "A", "B", "C" }, "A、B、C")]
    [InlineData("list", "en", new[] { "{1}", "{0}" }, "{1} and {0}")]
    [InlineData("list", "en", new[] { "a" }, "a")]
    [InlineData("list", "en", new string[0], "")]
    public void Format_List_ShouldMatchIntl(string format, string language, string[] value, string expected)
    {
        _formatter.CanFormat(value, format, language).ShouldBeTrue();

        _formatter.Format(value, format, language).ShouldBe(expected);
    }

    [Fact]
    public void Format_ListOfOtherValues_ShouldUseToString()
    {
        _formatter.Format(new List<object> { 1, null, 2.5 }, "list", "en").ShouldBe("1, , and 2.5");
        _formatter.CanFormat("abc", "list", "en").ShouldBeFalse();
        Should.Throw<ArgumentException>(() => _formatter.Format(new[] { "a", "b" }, "list(type: unknown)", "en"));
    }

    [Fact]
    public async Task Interpolator_RelativeTimeAndList_ShouldBeAvailableWithoutRegistration()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        var args = new Dictionary<string, object> { ["days"] = -1, ["names"] = new[] { "Anna", "Ben", "Carl" } };

        var result = await interpolator.InterpolateAsync("Updated {{days, relativetime(numeric: auto)}} by {{names, list}}", "key", "en", args);

        result.ShouldBe("Updated yesterday by Anna, Ben, and Carl");
    }
}
