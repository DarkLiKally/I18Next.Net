using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters;

[TestFixture]
public class DateFnsFormatterFixture
{
    private static readonly DateFnsFormatter Formatter = new();

    public static IEnumerable Cases
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles", "date-fns", "format.json")));

            foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var locale = testCase.GetProperty("locale").GetString();
                var date = testCase.GetProperty("date").GetString();
                var format = testCase.GetProperty("format").GetString();

                yield return new TestCaseData(locale, date, format)
                    .Returns(testCase.GetProperty("expected").GetString())
                    .SetName($"{locale} {date} {format}");
            }
        }
    }

    [TestCaseSource(nameof(Cases))]
    public string Format_ShouldMatchDateFns(string locale, string date, string format)
    {
        return Formatter.Format(DateTimeOffset.Parse(date), format, locale);
    }

    [TestCase("en", ExpectedResult = "January 25th, 2018")]
    [TestCase("de-DE", ExpectedResult = "25. Januar 2018")]
    [TestCase("pt", ExpectedResult = "25 de janeiro de 2018")]
    [TestCase("zh", ExpectedResult = "2018年1月25日")]
    [TestCase("unknown", ExpectedResult = "January 25th, 2018")]
    public string Format_LocaleResolution_ShouldFallBackToLanguageOrEnglish(string language)
    {
        return Formatter.Format(new DateTime(2018, 1, 25), "PPP", language);
    }

    [Test]
    public void Format_WeekOptions_ShouldOverrideLocale()
    {
        var date = new DateTime(2021, 1, 3);

        Formatter.Format(date, "w e", "en-US").Should().Be("2 1");
        new DateFnsFormatter { WeekStartsOn = 1, FirstWeekContainsDate = 4 }.Format(date, "w e", "en-US").Should().Be("53 7");
    }

    [Test]
    public void Format_UtcDateTime_ShouldUseZeroOffset()
    {
        Formatter.Format(new DateTime(2018, 1, 25, 7, 37, 59, DateTimeKind.Utc), "HH:mm X O t", "en").Should().Be("07:37 Z GMT+0 1516865879");
    }

    [Test]
    public void CanFormat_ShouldOnlyAcceptDatesAndNonIntlFormats()
    {
        Formatter.CanFormat(DateTime.Now, "yyyy", "en").Should().BeTrue();
        Formatter.CanFormat(DateTimeOffset.Now, "PPP", "en").Should().BeTrue();
        Formatter.CanFormat(DateTime.Now, "datetime", "en").Should().BeFalse();
        Formatter.CanFormat(DateTime.Now, null, "en").Should().BeFalse();
        Formatter.CanFormat("2018", "yyyy", "en").Should().BeFalse();
    }

    [Test]
    public void Format_UnknownLetters_ShouldBeKeptAsText()
    {
        Formatter.Format(new DateTime(2018, 1, 25), "yyyy 'J' ffff", "en").Should().Be("2018 J ffff");
    }

    [Test]
    public async Task Interpolator_DateFnsFormatter_ShouldFormatDatesAndLeaveIntlFormats()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        interpolator.Formatters.Add(new DateFnsFormatter());

        var args = new Dictionary<string, object> { ["date"] = new DateTime(2018, 1, 25, 7, 37, 0) };

        var result = await interpolator.InterpolateAsync("{{date, EEEE do MMMM}} / {{date, datetime(dateStyle: medium)}}", "key", "en-US", args);

        result.Should().Be("Thursday 25th January / Jan 25, 2018");
    }
}
