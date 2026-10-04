using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultPluralResolverCldrFixture
{
    private static IEnumerable<object[]> GetSamples(string kind)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestFiles", "cldr", "plural-samples.json")));

        foreach (var language in document.RootElement.GetProperty(kind).EnumerateObject())
        {
            foreach (var category in language.Value.EnumerateObject())
            {
                foreach (var sample in category.Value.EnumerateArray())
                {
                    yield return sample.ValueKind == JsonValueKind.String
                        ? new object[] { language.Name, sample.GetString(), category.Name }
                        : new object[] { language.Name, sample.GetInt32(), category.Name };
                }
            }
        }
    }

    public static IEnumerable<object[]> CardinalSamples => GetSamples("cardinal");

    public static IEnumerable<object[]> OrdinalSamples => GetSamples("ordinal");

    public static IEnumerable<object[]> DecimalSamples => GetSamples("decimal");

    [Theory]
    [MemberData(nameof(CardinalSamples), DisableDiscoveryEnumeration = true)]
    public void GetPluralCategory_CldrSamples_ShouldMatch(string language, int count, string expected)
    {
        DefaultPluralResolver.GetPluralCategory(language, count).ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(OrdinalSamples), DisableDiscoveryEnumeration = true)]
    public void GetOrdinalPluralCategory_CldrSamples_ShouldMatch(string language, int count, string expected)
    {
        DefaultPluralResolver.GetOrdinalPluralCategory(language, count).ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(DecimalSamples), DisableDiscoveryEnumeration = true)]
    public void GetPluralCategory_CldrDecimalSamples_ShouldMatch(string language, string count, string expected)
    {
        DefaultPluralResolver.GetPluralCategory(language, decimal.Parse(count, CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", 1, "one")]
    [InlineData("en", -1, "one")]
    [InlineData("ru", 5, "many")]
    [InlineData("unknown", 1.5, "other")]
    [InlineData("pt-BR", 1.5, "one")]
    [InlineData("fr-CA", 0.5, "one")]
    public void GetPluralCategory_Decimal_ShouldUseIntegerRulesForWholeNumbers(string language, double count, string expected)
    {
        DefaultPluralResolver.GetPluralCategory(language, (decimal)count).ShouldBe(expected);
    }
}
