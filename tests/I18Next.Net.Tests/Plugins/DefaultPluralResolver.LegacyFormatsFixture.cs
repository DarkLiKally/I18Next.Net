using FluentAssertions;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class DefaultPluralResolverLegacyFormatsFixture
{
    private static string GetSuffix(JsonFormat jsonFormat, string language, int count)
    {
        return new DefaultPluralResolver { JsonFormatVersion = jsonFormat }.GetPluralSuffix(language, count);
    }

    [TestCase("en", 0, ExpectedResult = "_plural")]
    [TestCase("en", 1, ExpectedResult = "")]
    [TestCase("en", 2, ExpectedResult = "_plural")]
    [TestCase("de-DE", 5, ExpectedResult = "_plural")]
    [TestCase("fr", 0, ExpectedResult = "")]
    [TestCase("fr", 2, ExpectedResult = "_plural")]
    [TestCase("ru", 1, ExpectedResult = "")]
    [TestCase("ru", 2, ExpectedResult = "_plural_2")]
    [TestCase("ru", 5, ExpectedResult = "_plural_5")]
    [TestCase("ru", 11, ExpectedResult = "_plural_5")]
    [TestCase("ru", 21, ExpectedResult = "")]
    [TestCase("ru", 22, ExpectedResult = "_plural_2")]
    [TestCase("ar", 0, ExpectedResult = "_plural_0")]
    [TestCase("ar", 1, ExpectedResult = "")]
    [TestCase("ar", 2, ExpectedResult = "_plural_2")]
    [TestCase("ar", 3, ExpectedResult = "_plural_3")]
    [TestCase("ar", 11, ExpectedResult = "_plural_11")]
    [TestCase("ar", 100, ExpectedResult = "_plural_100")]
    [TestCase("pl", 1, ExpectedResult = "")]
    [TestCase("pl", 2, ExpectedResult = "_plural_2")]
    [TestCase("pl", 12, ExpectedResult = "_plural_5")]
    [TestCase("sl", 1, ExpectedResult = "")]
    [TestCase("sl", 2, ExpectedResult = "_plural_2")]
    [TestCase("sl", 3, ExpectedResult = "_plural_3")]
    [TestCase("sl", 5, ExpectedResult = "_plural_5")]
    [TestCase("ja", 1, ExpectedResult = "")]
    [TestCase("unknown", 2, ExpectedResult = "")]
    public string GetPluralSuffix_Version1_ShouldMatchI18Next(string language, int count)
    {
        return GetSuffix(JsonFormat.Version1, language, count);
    }

    [TestCase("en", 1, ExpectedResult = "")]
    [TestCase("en", 2, ExpectedResult = "_plural")]
    [TestCase("ru", 1, ExpectedResult = "_1")]
    [TestCase("ru", 2, ExpectedResult = "_2")]
    [TestCase("ru", 5, ExpectedResult = "_5")]
    [TestCase("ru", 0, ExpectedResult = "_5")]
    [TestCase("ru", 21, ExpectedResult = "_1")]
    [TestCase("ar", 0, ExpectedResult = "_0")]
    [TestCase("ar", 1, ExpectedResult = "_1")]
    [TestCase("ar", 2, ExpectedResult = "_2")]
    [TestCase("ar", 3, ExpectedResult = "_3")]
    [TestCase("ar", 11, ExpectedResult = "_11")]
    [TestCase("ar", 100, ExpectedResult = "_100")]
    [TestCase("pl", 1, ExpectedResult = "_1")]
    [TestCase("pl", 3, ExpectedResult = "_2")]
    [TestCase("pl", 5, ExpectedResult = "_5")]
    [TestCase("sl", 5, ExpectedResult = "_5")]
    [TestCase("sl", 101, ExpectedResult = "_1")]
    [TestCase("sl", 102, ExpectedResult = "_2")]
    [TestCase("sl", 104, ExpectedResult = "_3")]
    [TestCase("unknown", 2, ExpectedResult = "")]
    public string GetPluralSuffix_Version2_ShouldMatchI18Next(string language, int count)
    {
        return GetSuffix(JsonFormat.Version2, language, count);
    }

    [TestCase("en", 0, ExpectedResult = "_plural")]
    [TestCase("en", 1, ExpectedResult = "")]
    [TestCase("en", 2, ExpectedResult = "_plural")]
    [TestCase("ru", 0, ExpectedResult = "_2")]
    [TestCase("ru", 1, ExpectedResult = "_0")]
    [TestCase("ru", 2, ExpectedResult = "_1")]
    [TestCase("ru", 5, ExpectedResult = "_2")]
    [TestCase("ru", 11, ExpectedResult = "_2")]
    [TestCase("ru", 21, ExpectedResult = "_0")]
    [TestCase("ru", 22, ExpectedResult = "_1")]
    [TestCase("ru", 111, ExpectedResult = "_2")]
    [TestCase("ar", 0, ExpectedResult = "_0")]
    [TestCase("ar", 1, ExpectedResult = "_1")]
    [TestCase("ar", 2, ExpectedResult = "_2")]
    [TestCase("ar", 3, ExpectedResult = "_3")]
    [TestCase("ar", 10, ExpectedResult = "_3")]
    [TestCase("ar", 11, ExpectedResult = "_4")]
    [TestCase("ar", 99, ExpectedResult = "_4")]
    [TestCase("ar", 100, ExpectedResult = "_5")]
    [TestCase("ar", 102, ExpectedResult = "_5")]
    [TestCase("pl", 1, ExpectedResult = "_0")]
    [TestCase("pl", 2, ExpectedResult = "_1")]
    [TestCase("pl", 5, ExpectedResult = "_2")]
    [TestCase("pl", 12, ExpectedResult = "_2")]
    [TestCase("pl", 21, ExpectedResult = "_2")]
    [TestCase("pl", 22, ExpectedResult = "_1")]
    [TestCase("cs", 1, ExpectedResult = "_0")]
    [TestCase("cs", 3, ExpectedResult = "_1")]
    [TestCase("cs", 5, ExpectedResult = "_2")]
    [TestCase("sl", 1, ExpectedResult = "_1")]
    [TestCase("sl", 2, ExpectedResult = "_2")]
    [TestCase("sl", 4, ExpectedResult = "_3")]
    [TestCase("sl", 5, ExpectedResult = "_0")]
    [TestCase("lv", 0, ExpectedResult = "_2")]
    [TestCase("lv", 1, ExpectedResult = "_0")]
    [TestCase("lv", 2, ExpectedResult = "_1")]
    [TestCase("ga", 7, ExpectedResult = "_3")]
    [TestCase("cy", 8, ExpectedResult = "_3")]
    [TestCase("ja", 5, ExpectedResult = "_0")]
    [TestCase("fr", 0, ExpectedResult = "")]
    [TestCase("fr", 2, ExpectedResult = "_plural")]
    [TestCase("pt-BR", 0, ExpectedResult = "")]
    [TestCase("de-AT", 2, ExpectedResult = "_plural")]
    [TestCase("unknown", 2, ExpectedResult = "")]
    public string GetPluralSuffix_Version3_ShouldMatchI18Next(string language, int count)
    {
        return GetSuffix(JsonFormat.Version3, language, count);
    }

    [TestCase(JsonFormat.Version1, "ru", -1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version2, "ru", -2, ExpectedResult = "_2")]
    [TestCase(JsonFormat.Version3, "ru", -5, ExpectedResult = "_2")]
    [TestCase(JsonFormat.Version3, "en", -1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version3, "en", int.MinValue, ExpectedResult = "_plural")]
    public string GetPluralSuffix_NegativeCounts_ShouldUseAbsoluteValue(JsonFormat jsonFormat, string language, int count)
    {
        return GetSuffix(jsonFormat, language, count);
    }

    [Test]
    public void GetPluralSuffix_CustomSeparator_ShouldBeUsedForVersion2And3()
    {
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version2, PluralSeparator = "|" }.GetPluralSuffix("ru", 2).Should().Be("|2");
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version3, PluralSeparator = "|" }.GetPluralSuffix("ru", 2).Should().Be("|1");
        new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version3, PluralSeparator = "|" }.GetPluralSuffix("en", 2).Should().Be("|plural");
    }

    [TestCase(JsonFormat.Version1, "en", ExpectedResult = true)]
    [TestCase(JsonFormat.Version1, "ja", ExpectedResult = false)]
    [TestCase(JsonFormat.Version2, "ru", ExpectedResult = true)]
    [TestCase(JsonFormat.Version2, "ja", ExpectedResult = false)]
    [TestCase(JsonFormat.Version2, "unknown", ExpectedResult = false)]
    [TestCase(JsonFormat.Version3, "ja", ExpectedResult = true)]
    [TestCase(JsonFormat.Version3, "unknown", ExpectedResult = true)]
    public bool NeedsPlural_ShouldDependOnFormatAndLanguage(JsonFormat jsonFormat, string language)
    {
        return new DefaultPluralResolver { JsonFormatVersion = jsonFormat }.NeedsPlural(language);
    }
}
