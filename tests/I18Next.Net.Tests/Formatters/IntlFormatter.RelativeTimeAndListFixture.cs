using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters;

[TestFixture]
public class IntlFormatterRelativeTimeAndListFixture
{
    private IntlFormatter _formatter;

    [SetUp]
    public void SetUp()
    {
        _formatter = new IntlFormatter();
    }

    [TestCase(3, "relativetime", "en", ExpectedResult = "in 3 days")]
    [TestCase(-3, "relativetime", "en", ExpectedResult = "3 days ago")]
    [TestCase(1, "relativetime", "en", ExpectedResult = "in 1 day")]
    [TestCase(-1, "relativetime(numeric: auto)", "en", ExpectedResult = "yesterday")]
    [TestCase(0, "relativetime(numeric: auto)", "en", ExpectedResult = "today")]
    [TestCase(1, "relativetime(numeric: auto)", "en", ExpectedResult = "tomorrow")]
    [TestCase(5, "relativetime(numeric: auto)", "en", ExpectedResult = "in 5 days")]
    [TestCase(-1, "relativetime(numeric: always)", "en", ExpectedResult = "1 day ago")]
    [TestCase(2, "relativetime(quarter)", "en", ExpectedResult = "in 2 quarters")]
    [TestCase(2, "relativetime(quarters)", "en", ExpectedResult = "in 2 quarters")]
    [TestCase(-2, "relativetime(range: week; style: short)", "en", ExpectedResult = "2 wk. ago")]
    [TestCase(5, "relativetime(hour; style: narrow)", "en", ExpectedResult = "in 5h")]
    [TestCase(10, "relativetime(minute)", "en", ExpectedResult = "in 10 minutes")]
    [TestCase(-30, "relativetime(second)", "en", ExpectedResult = "30 seconds ago")]
    [TestCase(-1, "relativetime(year; numeric: auto)", "en", ExpectedResult = "last year")]
    [TestCase(1, "relativetime(month; numeric: auto)", "en", ExpectedResult = "next month")]
    [TestCase(-1, "relativetime(numeric: auto)", "de", ExpectedResult = "gestern")]
    [TestCase(-2, "relativetime(numeric: auto)", "de-AT", ExpectedResult = "vorgestern")]
    [TestCase(1500, "relativetime(day)", "de-DE", ExpectedResult = "in 1.500 Tagen")]
    [TestCase(3, "relativetime(month)", "fr", ExpectedResult = "dans 3 mois")]
    [TestCase(-5, "relativetime(year)", "ru", ExpectedResult = "5 лет назад")]
    [TestCase(2, "relativetime(day)", "ru", ExpectedResult = "через 2 дня")]
    [TestCase(21, "relativetime(day)", "ru", ExpectedResult = "через 21 день")]
    [TestCase(3, "relativetime", "zh-TW", ExpectedResult = "3 天後")]
    [TestCase(3, "relativetime", "zh", ExpectedResult = "3天后")]
    [TestCase(3, "relativetime", "unknown", ExpectedResult = "in 3 days")]
    [TestCase(1.5, "relativetime(hour)", "en", ExpectedResult = "in 1.5 hours")]
    public string Format_RelativeTime_ShouldMatchIntl(double value, string format, string language)
    {
        _formatter.CanFormat(value, format, language).Should().BeTrue();

        return _formatter.Format(value, format, language);
    }

    [Test]
    public void Format_RelativeTimeNegativeZero_ShouldBePast()
    {
        _formatter.Format(-0.0, "relativetime", "en").Should().Be("0 days ago");
        _formatter.Format(0, "relativetime", "en").Should().Be("in 0 days");
    }

    [Test]
    public void Format_RelativeTimeUnknownUnit_ShouldThrow()
    {
        _formatter.Invoking(f => f.Format(1, "relativetime(fortnight)", "en")).Should().Throw<ArgumentException>();
    }

    [TestCase("list", "en", new[] { "a", "b", "c" }, ExpectedResult = "a, b, and c")]
    [TestCase("list", "en", new[] { "a", "b" }, ExpectedResult = "a and b")]
    [TestCase("list", "en", new[] { "a", "b", "c", "d" }, ExpectedResult = "a, b, c, and d")]
    [TestCase("list", "en-GB", new[] { "a", "b", "c" }, ExpectedResult = "a, b and c")]
    [TestCase("list(type: disjunction)", "en", new[] { "a", "b", "c" }, ExpectedResult = "a, b, or c")]
    [TestCase("list(type: disjunction)", "de", new[] { "a", "b", "c", "d" }, ExpectedResult = "a, b, c oder d")]
    [TestCase("list(type: unit)", "en", new[] { "5 hours", "3 minutes" }, ExpectedResult = "5 hours, 3 minutes")]
    [TestCase("list(type: unit; style: narrow)", "en", new[] { "a", "b", "c" }, ExpectedResult = "a b c")]
    [TestCase("list(style: short)", "en", new[] { "a", "b", "c" }, ExpectedResult = "a, b, & c")]
    [TestCase("list", "es", new[] { "a", "b", "c" }, ExpectedResult = "a, b y c")]
    [TestCase("list", "ja", new[] { "A", "B", "C" }, ExpectedResult = "A、B、C")]
    [TestCase("list", "en", new[] { "{1}", "{0}" }, ExpectedResult = "{1} and {0}")]
    [TestCase("list", "en", new[] { "a" }, ExpectedResult = "a")]
    [TestCase("list", "en", new string[0], ExpectedResult = "")]
    public string Format_List_ShouldMatchIntl(string format, string language, string[] value)
    {
        _formatter.CanFormat(value, format, language).Should().BeTrue();

        return _formatter.Format(value, format, language);
    }

    [Test]
    public void Format_ListOfOtherValues_ShouldUseToString()
    {
        _formatter.Format(new List<object> { 1, null, 2.5 }, "list", "en").Should().Be("1, , and 2.5");
        _formatter.CanFormat("abc", "list", "en").Should().BeFalse();
        _formatter.Invoking(f => f.Format(new[] { "a", "b" }, "list(type: unknown)", "en")).Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task Interpolator_RelativeTimeAndList_ShouldBeAvailableWithoutRegistration()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        var args = new Dictionary<string, object> { ["days"] = -1, ["names"] = new[] { "Anna", "Ben", "Carl" } };

        var result = await interpolator.InterpolateAsync("Updated {{days, relativetime(numeric: auto)}} by {{names, list}}", "key", "en", args);

        result.Should().Be("Updated yesterday by Anna, Ben, and Carl");
    }
}
