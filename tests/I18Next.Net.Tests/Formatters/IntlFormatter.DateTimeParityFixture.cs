using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using I18Next.Net.Formatters;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Formatters;

public class IntlFormatterDateTimeParityFixture
{
    private static readonly HashSet<string> KnownDifferences = new()
    {
        "th|datetime(hour: numeric; minute: 2-digit)",
        "th|datetime(hour: 2-digit; minute: 2-digit)",
        "th|datetime(hour: numeric; minute: 2-digit; hour12: false)",
        "th|datetime(year: numeric; month: long; day: numeric; hour: numeric; minute: 2-digit)",
        "th|datetime(weekday: long; year: numeric; month: long; day: numeric; hour: numeric; minute: 2-digit)",
        "th|datetime(year: numeric; month: numeric; day: numeric; hour: numeric; minute: 2-digit)",
        "fi|datetime(weekday: long; year: numeric; month: long; day: numeric; hour: numeric; minute: 2-digit)",
        "fa|datetime(hour: numeric; minute: 2-digit; timeZoneName: short)"
    };

    private static readonly IntlFormatter Formatter = new();

    public static IEnumerable<object[]> Cases
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestFiles", "intl", "datetime.json")));

            foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var locale = testCase.GetProperty("locale").GetString();
                var format = testCase.GetProperty("format").GetString();

                if (KnownDifferences.Contains($"{locale}|{format}"))
                    continue;

                var date = testCase.GetProperty("date").GetString();

                yield return new object[] { locale, date, format, testCase.GetProperty("expected").GetString() };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases), DisableDiscoveryEnumeration = true)]
    public void Format_DateTime_ShouldMatchBrowserIntl(string locale, string date, string format, string expected)
    {
        Formatter.Format(DateTimeOffset.Parse(date), format, locale).ShouldBe(expected);
    }
}
