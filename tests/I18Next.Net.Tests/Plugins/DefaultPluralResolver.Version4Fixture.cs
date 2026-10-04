using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultPluralResolverVersion4Fixture
{
    public DefaultPluralResolverVersion4Fixture()
    {
        _pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
    }
    private readonly DefaultPluralResolver _pluralResolver;


    [Theory]
    [InlineData("en", 0, "_other")]
    [InlineData("en", 1, "_one")]
    [InlineData("en", 2, "_other")]
    [InlineData("en-US", 1, "_one")]
    [InlineData("de-DE", 5, "_other")]
    [InlineData("dev", 1, "_one")]
    [InlineData("fr", 0, "_one")]
    [InlineData("fr", 1, "_one")]
    [InlineData("fr", 2, "_other")]
    [InlineData("fr", 1000000, "_many")]
    [InlineData("es", 1, "_one")]
    [InlineData("es", 2000000, "_many")]
    [InlineData("pt-PT", 0, "_other")]
    [InlineData("pt-BR", 0, "_one")]
    [InlineData("ja", 1, "_other")]
    [InlineData("zh", 5, "_other")]
    [InlineData("ru", 1, "_one")]
    [InlineData("ru", 2, "_few")]
    [InlineData("ru", 5, "_many")]
    [InlineData("ru", 11, "_many")]
    [InlineData("ru", 21, "_one")]
    [InlineData("ru", 22, "_few")]
    [InlineData("pl", 1, "_one")]
    [InlineData("pl", 3, "_few")]
    [InlineData("pl", 13, "_many")]
    [InlineData("pl", 21, "_many")]
    [InlineData("cs", 3, "_few")]
    [InlineData("cs", 5, "_other")]
    [InlineData("ar", 0, "_zero")]
    [InlineData("ar", 1, "_one")]
    [InlineData("ar", 2, "_two")]
    [InlineData("ar", 3, "_few")]
    [InlineData("ar", 11, "_many")]
    [InlineData("ar", 100, "_other")]
    [InlineData("cy", 3, "_few")]
    [InlineData("cy", 6, "_many")]
    [InlineData("lv", 0, "_zero")]
    [InlineData("lv", 21, "_one")]
    [InlineData("lv", 2, "_other")]
    [InlineData("sl", 102, "_two")]
    [InlineData("ga", 7, "_many")]
    [InlineData("he", 2, "_two")]
    [InlineData("ro", 0, "_few")]
    [InlineData("ro", 20, "_other")]
    [InlineData("lt", 11, "_other")]
    [InlineData("lt", 2, "_few")]
    [InlineData("mt", 0, "_few")]
    [InlineData("mt", 15, "_many")]
    [InlineData("br", 1000000, "_many")]
    [InlineData("gv", 40, "_few")]
    [InlineData("kw", 22, "_two")]
    [InlineData("en", -1, "_one")]
    [InlineData("unknown", 1, "_other")]
    public void GetPluralSuffix_ShouldReturnCldrCategorySuffix(string language, int count, string expected)
    {
        _pluralResolver.GetPluralSuffix(language, count).ShouldBe(expected);
    }

    [Fact]
    public void GetPluralSuffix_CustomSeparator_ShouldUseSeparator()
    {
        _pluralResolver.PluralSeparator = "|";

        _pluralResolver.GetPluralSuffix("en", 2).ShouldBe("|other");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ja")]
    [InlineData("unknown")]
    public void NeedsPlural_ShouldAlwaysBeTrue(string language)
    {
        _pluralResolver.NeedsPlural(language).ShouldBeTrue();
    }

    [Fact]
    public void GetPluralCategory_ShouldReturnCategoryWithoutSeparator()
    {
        DefaultPluralResolver.GetPluralCategory("en", 1).ShouldBe("one");
        DefaultPluralResolver.GetPluralCategory("en", int.MinValue).ShouldBe("other");
    }
}
