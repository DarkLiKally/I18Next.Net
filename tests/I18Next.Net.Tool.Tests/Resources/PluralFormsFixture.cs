using System.Collections.Generic;

using I18Next.Net.Tool.Resources;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Resources;

public class PluralFormsFixture
{
    [Theory]
    [InlineData("en", new[] { "one", "other" })]
    [InlineData("en-US", new[] { "one", "other" })]
    [InlineData("de", new[] { "one", "other" })]
    [InlineData("ja", new[] { "other" })]
    [InlineData("fr", new[] { "one", "many", "other" })]
    [InlineData("ru", new[] { "one", "few", "many", "other" })]
    [InlineData("pl", new[] { "one", "few", "many", "other" })]
    [InlineData("ar", new[] { "zero", "one", "two", "few", "many", "other" })]
    [InlineData("xx", new[] { "other" })]
    public void GetCategories_ShouldReturnTheCldrCategories(string language, string[] expected)
    {
        PluralForms.GetCategories(language).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", new[] { "one", "two", "few", "other" })]
    [InlineData("de", new[] { "other" })]
    [InlineData("sv", new[] { "one", "other" })]
    public void GetCategories_Ordinal_ShouldReturnTheOrdinalCategories(string language, string[] expected)
    {
        PluralForms.GetCategories(language, true).ShouldBe(expected);
    }

    [Fact]
    public void GetKeys_ShouldAppendTheSuffixes()
    {
        PluralForms.GetKeys("item", "en", false).ShouldBe(["item_one", "item_other"]);
        PluralForms.GetKeys("place", "en", true).ShouldBe(["place_ordinal_one", "place_ordinal_two", "place_ordinal_few", "place_ordinal_other"]);
    }

    [Fact]
    public void TryParse_ShouldRequireTheOtherForm()
    {
        var keys = new HashSet<string> { "item_one", "item_other", "place_ordinal_two", "place_ordinal_other", "button_one" };

        PluralForms.TryParse("item_one", keys.Contains, out var item).ShouldBeTrue();
        item.BaseKey.ShouldBe("item");
        item.Category.ShouldBe("one");
        item.Ordinal.ShouldBeFalse();
        item.GetKey("few").ShouldBe("item_few");

        PluralForms.TryParse("place_ordinal_two", keys.Contains, out var place).ShouldBeTrue();
        place.BaseKey.ShouldBe("place");
        place.Ordinal.ShouldBeTrue();
        place.GetKey("few").ShouldBe("place_ordinal_few");

        PluralForms.TryParse("place_ordinal_other", keys.Contains, out place).ShouldBeTrue();
        place.Category.ShouldBe("other");

        PluralForms.TryParse("button_one", keys.Contains, out var button).ShouldBeFalse();
        button.ShouldBeNull();
        PluralForms.TryParse("_other", keys.Contains, out _).ShouldBeFalse();
        PluralForms.TryParse("title", keys.Contains, out _).ShouldBeFalse();
    }
}
