using FluentAssertions;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class DefaultPluralResolverVersion4Fixture
{
    private DefaultPluralResolver _pluralResolver;

    [SetUp]
    public void SetUp()
    {
        _pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
    }

    [TestCase("en", 0, ExpectedResult = "_other")]
    [TestCase("en", 1, ExpectedResult = "_one")]
    [TestCase("en", 2, ExpectedResult = "_other")]
    [TestCase("en-US", 1, ExpectedResult = "_one")]
    [TestCase("de-DE", 5, ExpectedResult = "_other")]
    [TestCase("dev", 1, ExpectedResult = "_one")]
    [TestCase("fr", 0, ExpectedResult = "_one")]
    [TestCase("fr", 1, ExpectedResult = "_one")]
    [TestCase("fr", 2, ExpectedResult = "_other")]
    [TestCase("fr", 1000000, ExpectedResult = "_many")]
    [TestCase("es", 1, ExpectedResult = "_one")]
    [TestCase("es", 2000000, ExpectedResult = "_many")]
    [TestCase("pt-PT", 0, ExpectedResult = "_other")]
    [TestCase("pt-BR", 0, ExpectedResult = "_one")]
    [TestCase("ja", 1, ExpectedResult = "_other")]
    [TestCase("zh", 5, ExpectedResult = "_other")]
    [TestCase("ru", 1, ExpectedResult = "_one")]
    [TestCase("ru", 2, ExpectedResult = "_few")]
    [TestCase("ru", 5, ExpectedResult = "_many")]
    [TestCase("ru", 11, ExpectedResult = "_many")]
    [TestCase("ru", 21, ExpectedResult = "_one")]
    [TestCase("ru", 22, ExpectedResult = "_few")]
    [TestCase("pl", 1, ExpectedResult = "_one")]
    [TestCase("pl", 3, ExpectedResult = "_few")]
    [TestCase("pl", 13, ExpectedResult = "_many")]
    [TestCase("pl", 21, ExpectedResult = "_many")]
    [TestCase("cs", 3, ExpectedResult = "_few")]
    [TestCase("cs", 5, ExpectedResult = "_other")]
    [TestCase("ar", 0, ExpectedResult = "_zero")]
    [TestCase("ar", 1, ExpectedResult = "_one")]
    [TestCase("ar", 2, ExpectedResult = "_two")]
    [TestCase("ar", 3, ExpectedResult = "_few")]
    [TestCase("ar", 11, ExpectedResult = "_many")]
    [TestCase("ar", 100, ExpectedResult = "_other")]
    [TestCase("cy", 3, ExpectedResult = "_few")]
    [TestCase("cy", 6, ExpectedResult = "_many")]
    [TestCase("lv", 0, ExpectedResult = "_zero")]
    [TestCase("lv", 21, ExpectedResult = "_one")]
    [TestCase("lv", 2, ExpectedResult = "_other")]
    [TestCase("sl", 102, ExpectedResult = "_two")]
    [TestCase("ga", 7, ExpectedResult = "_many")]
    [TestCase("he", 2, ExpectedResult = "_two")]
    [TestCase("ro", 0, ExpectedResult = "_few")]
    [TestCase("ro", 20, ExpectedResult = "_other")]
    [TestCase("lt", 11, ExpectedResult = "_other")]
    [TestCase("lt", 2, ExpectedResult = "_few")]
    [TestCase("mt", 0, ExpectedResult = "_few")]
    [TestCase("mt", 15, ExpectedResult = "_many")]
    [TestCase("br", 1000000, ExpectedResult = "_many")]
    [TestCase("gv", 40, ExpectedResult = "_few")]
    [TestCase("kw", 22, ExpectedResult = "_two")]
    [TestCase("en", -1, ExpectedResult = "_one")]
    [TestCase("unknown", 1, ExpectedResult = "_other")]
    public string GetPluralSuffix_ShouldReturnCldrCategorySuffix(string language, int count)
    {
        return _pluralResolver.GetPluralSuffix(language, count);
    }

    [Test]
    public void GetPluralSuffix_CustomSeparator_ShouldUseSeparator()
    {
        _pluralResolver.PluralSeparator = "|";

        _pluralResolver.GetPluralSuffix("en", 2).Should().Be("|other");
    }

    [TestCase("en")]
    [TestCase("ja")]
    [TestCase("unknown")]
    public void NeedsPlural_ShouldAlwaysBeTrue(string language)
    {
        _pluralResolver.NeedsPlural(language).Should().BeTrue();
    }

    [Test]
    public void GetPluralCategory_ShouldReturnCategoryWithoutSeparator()
    {
        DefaultPluralResolver.GetPluralCategory("en", 1).Should().Be("one");
        DefaultPluralResolver.GetPluralCategory("en", int.MinValue).Should().Be("other");
    }
}
