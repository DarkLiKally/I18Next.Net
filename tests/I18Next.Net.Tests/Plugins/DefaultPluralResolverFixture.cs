using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultPluralResolverFixture
{
    [Theory]
    [InlineData(JsonFormat.Version1, "")]
    [InlineData(JsonFormat.Version2, "")]
    [InlineData(JsonFormat.Version3, "")]
    public void GetPluralSuffix_OneInEnglish_ShouldReturnEmptyWhenUsingSimpleSuffix(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = true
        };

        pluralResolver.GetPluralSuffix("en", 1).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "")]
    [InlineData(JsonFormat.Version2, "_1")]
    [InlineData(JsonFormat.Version3, "_0")]
    public void GetPluralSuffix_OneInEnglish_ShouldReturnNumberWhenNotUsingSimpleSuffix(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = false
        };

        pluralResolver.GetPluralSuffix("en", 1).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "_plural")]
    [InlineData(JsonFormat.Version2, "_plural")]
    [InlineData(JsonFormat.Version3, "_plural")]
    public void GetPluralSuffix_TwoInEnglish_ShouldReturnPluralWhenUsingSimpleSuffix(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = true
        };

        pluralResolver.GetPluralSuffix("en", 2).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "_plural_2")]
    [InlineData(JsonFormat.Version2, "_2")]
    [InlineData(JsonFormat.Version3, "_1")]
    public void GetPluralSuffix_TwoInEnglish_ShouldReturnNumberWhenNotUsingSimpleSuffix(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = false
        };

        pluralResolver.GetPluralSuffix("en", 2).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "")]
    [InlineData(JsonFormat.Version2, "")]
    [InlineData(JsonFormat.Version3, "_0")]
    public void GetPluralSuffix_OneInJapanese_ShouldReturnNumber(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
        };

        pluralResolver.GetPluralSuffix("ja", 1).ShouldBe(expected);
    }

    [Theory]
    [InlineData(JsonFormat.Version1, "")]
    [InlineData(JsonFormat.Version2, "")]
    [InlineData(JsonFormat.Version3, "_0")]
    public void GetPluralSuffix_TwoInJapanese_ShouldReturnNumber(JsonFormat jsonFormatVersion, string expected)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
        };

        pluralResolver.GetPluralSuffix("ja", 2).ShouldBe(expected);
    }
}