using Localizer.Plugins;
using NUnit.Framework;

namespace Localizer.Tests.Plugins;

[TestFixture]
public class DefaultPluralResolverFixture
{
    [TestCase(JsonFormat.Version1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "")]
    public string GetPluralSuffix_OneInEnglish_ShouldReturnEmptyWhenUsingSimpleSuffix(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = true
        };

        return pluralResolver.GetPluralSuffix("en", 1);
    }

    [TestCase(JsonFormat.Version1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "_1")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "_0")]
    public string GetPluralSuffix_OneInEnglish_ShouldReturnNumberWhenNotUsingSimpleSuffix(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = false
        };

        return pluralResolver.GetPluralSuffix("en", 1);
    }

    [TestCase(JsonFormat.Version1, ExpectedResult = "_plural")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "_plural")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "_plural")]
    public string GetPluralSuffix_TwoInEnglish_ShouldReturnPluralWhenUsingSimpleSuffix(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = true
        };

        return pluralResolver.GetPluralSuffix("en", 2);
    }

    [TestCase(JsonFormat.Version1, ExpectedResult = "_2")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "_2")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "_1")]
    public string GetPluralSuffix_TwoInEnglish_ShouldReturnNumberWhenNotUsingSimpleSuffix(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
            UseSimplePluralSuffixIfPossible = false
        };

        return pluralResolver.GetPluralSuffix("en", 2);
    }

    [TestCase(JsonFormat.Version1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "_0")]
    public string GetPluralSuffix_OneInJapanese_ShouldReturnNumber(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
        };

        return pluralResolver.GetPluralSuffix("ja", 1);
    }

    [TestCase(JsonFormat.Version1, ExpectedResult = "")]
    [TestCase(JsonFormat.Version2, ExpectedResult = "")]
    [TestCase(JsonFormat.Version3, ExpectedResult = "_0")]
    public string GetPluralSuffix_TwoInJapanese_ShouldReturnNumber(JsonFormat jsonFormatVersion)
    {
        var pluralResolver = new DefaultPluralResolver()
        {
            JsonFormatVersion = jsonFormatVersion,
        };

        return pluralResolver.GetPluralSuffix("ja", 2);
    }
}