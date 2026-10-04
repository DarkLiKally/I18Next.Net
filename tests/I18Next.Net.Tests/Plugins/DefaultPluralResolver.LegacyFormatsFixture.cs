using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultPluralResolverLegacyFormatsFixture
{
    private static string GetSuffix(JsonFormat jsonFormat, string language, int count)
    {
        return new DefaultPluralResolver { JsonFormatVersion = jsonFormat }.GetPluralSuffix(language, count);
    }

    [Theory]
    [InlineData("en", 0, "_plural")]
    [InlineData("en", 1, "")]
    [InlineData("en", 2, "_plural")]
    [InlineData("de-DE", 5, "_plural")]
    [InlineData("fr", 0, "")]
    [InlineData("fr", 2, "_plural")]
    [InlineData("ru", 1, "")]
    [InlineData("ru", 2, "_plural_2")]
    [InlineData("ru", 5, "_plural_5")]
    [InlineData("ru", 11, "_plural_5")]
    [InlineData("ru", 21, "")]
    [InlineData("ru", 22, "_plural_2")]
    [InlineData("ar", 0, "_plural_0")]
    [InlineData("ar", 1, "")]
    [InlineData("ar", 2, "_plural_2")]
    [InlineData("ar", 3, "_plural_3")]
    [InlineData("ar", 11, "_plural_11")]
    [InlineData("ar", 100, "_plural_100")]
    [InlineData("pl", 1, "")]
    [InlineData("pl", 2, "_plural_2")]
    [InlineData("pl", 12, "_plural_5")]
    [InlineData("sl", 1, "")]
    [InlineData("sl", 2, "_plural_2")]
    [InlineData("sl", 3, "_plural_3")]
    [InlineData("sl", 5, "_plural_5")]
    [InlineData("ja", 1, "")]
    [InlineData("unknown", 2, "")]
    public void GetPluralSuffix_Version1_ShouldMatchI18Next(string language, int count, string expected)
    {
        GetSuffix(JsonFormat.Version1, language, count).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", 1, "")]
    [InlineData("en", 2, "_plural")]
    [InlineData("ru", 1, "_1")]
    [InlineData("ru", 2, "_2")]
    [InlineData("ru", 5, "_5")]
    [InlineData("ru", 0, "_5")]
    [InlineData("ru", 21, "_1")]
    [InlineData("ar", 0, "_0")]
    [InlineData("ar", 1, "_1")]
    [InlineData("ar", 2, "_2")]
    [InlineData("ar", 3, "_3")]
    [InlineData("ar", 11, "_11")]
    [InlineData("ar", 100, "_100")]
    [InlineData("pl", 1, "_1")]
    [InlineData("pl", 3, "_2")]
    [InlineData("pl", 5, "_5")]
    [InlineData("sl", 5, "_5")]
    [InlineData("sl", 101, "_1")]
    [InlineData("sl", 102, "_2")]
    [InlineData("sl", 104, "_3")]
    [InlineData("unknown", 2, "")]
    public void GetPluralSuffix_Version2_ShouldMatchI18Next(string language, int count, string expected)
    {
        GetSuffix(JsonFormat.Version2, language, count).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", 0, "_plural")]
    [InlineData("en", 1, "")]
    [InlineData("en", 2, "_plural")]
    [InlineData("ru", 0, "_2")]
    [InlineData("ru", 1, "_0")]
    [InlineData("ru", 2, "_1")]
    [InlineData("ru", 5, "_2")]
    [InlineData("ru", 11, "_2")]
    [InlineData("ru", 21, "_0")]
    [InlineData("ru", 22, "_1")]
    [InlineData("ru", 111, "_2")]
    [InlineData("ar", 0, "_0")]
    [InlineData("ar", 1, "_1")]
    [InlineData("ar", 2, "_2")]
    [InlineData("ar", 3, "_3")]
    [InlineData("ar", 10, "_3")]
    [InlineData("ar", 11, "_4")]
    [InlineData("ar", 99, "_4")]
    [InlineData("ar", 100, "_5")]
    [InlineData("ar", 102, "_5")]
    [InlineData("pl", 1, "_0")]
    [InlineData("pl", 2, "_1")]
    [InlineData("pl", 5, "_2")]
    [InlineData("pl", 12, "_2")]
    [InlineData("pl", 21, "_2")]
    [InlineData("pl", 22, "_1")]
    [InlineData("cs", 1, "_0")]
    [InlineData("cs", 3, "_1")]
    [InlineData("cs", 5, "_2")]
    [InlineData("sl", 1, "_1")]
    [InlineData("sl", 2, "_2")]
    [InlineData("sl", 4, "_3")]
    [InlineData("sl", 5, "_0")]
    [InlineData("lv", 0, "_2")]
    [InlineData("lv", 1, "_0")]
    [InlineData("lv", 2, "_1")]
    [InlineData("ga", 7, "_3")]
    [InlineData("cy", 8, "_3")]
    [InlineData("ja", 5, "_0")]
    [InlineData("fr", 0, "")]
    [InlineData("fr", 2, "_plural")]
    [InlineData("pt-BR", 0, "")]
    [InlineData("de-AT", 2, "_plural")]
    [InlineData("unknown", 2, "")]
    public void GetPluralSuffix_Version3_ShouldMatchI18Next(string language, int count, string expected)
    {
        GetSuffix(JsonFormat.Version3, language, count).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "ru", -1, "")]
    [InlineData(JsonFormat.Version2, "ru", -2, "_2")]
    [InlineData(JsonFormat.Version3, "ru", -5, "_2")]
    [InlineData(JsonFormat.Version3, "en", -1, "")]
    [InlineData(JsonFormat.Version3, "en", int.MinValue, "_plural")]
    public void GetPluralSuffix_NegativeCounts_ShouldUseAbsoluteValue(JsonFormat jsonFormat, string language, int count, string expected)
    {
        GetSuffix(jsonFormat, language, count).ShouldBe(expected);
    }

    [Fact]
    public void GetPluralSuffix_CustomSeparator_ShouldBeUsedForVersion2And3()
    {
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version2, PluralSeparator = "|" }.GetPluralSuffix("ru", 2).ShouldBe("|2");
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version3, PluralSeparator = "|" }.GetPluralSuffix("ru", 2).ShouldBe("|1");
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version3, PluralSeparator = "|" }.GetPluralSuffix("en", 2).ShouldBe("|plural");
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "en", true)]
    [InlineData(JsonFormat.Version1, "ja", false)]
    [InlineData(JsonFormat.Version2, "ru", true)]
    [InlineData(JsonFormat.Version2, "ja", false)]
    [InlineData(JsonFormat.Version2, "unknown", false)]
    [InlineData(JsonFormat.Version3, "ja", true)]
    [InlineData(JsonFormat.Version3, "unknown", true)]
    public void NeedsPlural_ShouldDependOnFormatAndLanguage(JsonFormat jsonFormat, string language, bool expected)
    {
        new DefaultPluralResolver { JsonFormatVersion = jsonFormat }.NeedsPlural(language).ShouldBe(expected);
    }
}
