using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class DateFnsFormatterFixture
{
    private static readonly DateFnsFormatter Formatter = new();

    public static IEnumerable<object[]> Cases
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestFiles", "date-fns", "format.json")));

            foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var locale = testCase.GetProperty("locale").GetString();
                var date = testCase.GetProperty("date").GetString();
                var format = testCase.GetProperty("format").GetString();

                yield return new object[] { locale, date, format, testCase.GetProperty("expected").GetString() };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases), DisableDiscoveryEnumeration = true)]
    public void Format_ShouldMatchDateFns(string locale, string date, string format, string expected)
    {
        Formatter.Format(DateTimeOffset.Parse(date), format, locale).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", "January 25th, 2018")]
    [InlineData("de-DE", "25. Januar 2018")]
    [InlineData("pt", "25 de janeiro de 2018")]
    [InlineData("zh", "2018年1月25日")]
    [InlineData("unknown", "January 25th, 2018")]
    public void Format_LocaleResolution_ShouldFallBackToLanguageOrEnglish(string language, string expected)
    {
        Formatter.Format(new DateTime(2018, 1, 25), "PPP", language).ShouldBe(expected);
    }

    [Fact]
    public void Format_WeekOptions_ShouldOverrideLocale()
    {
        var date = new DateTime(2021, 1, 3);

        Formatter.Format(date, "w e", "en-US").ShouldBe("2 1");
        new DateFnsFormatter { WeekStartsOn = 1, FirstWeekContainsDate = 4 }.Format(date, "w e", "en-US").ShouldBe("53 7");
    }

    [Fact]
    public void Format_UtcDateTime_ShouldUseZeroOffset()
    {
        Formatter.Format(new DateTime(2018, 1, 25, 7, 37, 59, DateTimeKind.Utc), "HH:mm X O t", "en").ShouldBe("07:37 Z GMT+0 1516865879");
    }

    [Fact]
    public void CanFormat_ShouldOnlyAcceptDatesAndNonIntlFormats()
    {
        Formatter.CanFormat(DateTime.Now, "yyyy", "en").ShouldBeTrue();
        Formatter.CanFormat(DateTimeOffset.Now, "PPP", "en").ShouldBeTrue();
        Formatter.CanFormat(DateTime.Now, "datetime", "en").ShouldBeFalse();
        Formatter.CanFormat(DateTime.Now, null, "en").ShouldBeFalse();
        Formatter.CanFormat("2018", "yyyy", "en").ShouldBeFalse();
    }

    [Fact]
    public void Format_UnknownLetters_ShouldBeKeptAsText()
    {
        Formatter.Format(new DateTime(2018, 1, 25), "yyyy 'J' ffff", "en").ShouldBe("2018 J ffff");
    }

    [Fact]
    public async Task Interpolator_DateFnsFormatter_ShouldFormatDatesAndLeaveIntlFormats()
    {
        var interpolator = new DefaultInterpolator(new TraceLogger());
        interpolator.Formatters.Add(new DateFnsFormatter());

        var args = new Dictionary<string, object> { ["date"] = new DateTime(2018, 1, 25, 7, 37, 0) };

        var result = await interpolator.InterpolateAsync("{{date, EEEE do MMMM}} / {{date, datetime(dateStyle: medium)}}", "key", "en-US", args);

        result.ShouldBe("Thursday 25th January / Jan 25, 2018");
    }
}
