using System;
using System.Collections;
using System.Collections.Generic;
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
                    yield return new object[] { language.Name, sample.GetInt32(), category.Name };
            }
        }
    }

    public static IEnumerable<object[]> CardinalSamples => GetSamples("cardinal");

    public static IEnumerable<object[]> OrdinalSamples => GetSamples("ordinal");

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
}
