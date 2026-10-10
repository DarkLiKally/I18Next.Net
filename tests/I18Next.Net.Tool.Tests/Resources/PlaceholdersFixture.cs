using System.Linq;

using I18Next.Net.Tool.Resources;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Resources;

public class PlaceholdersFixture
{
    [Fact]
    public void Get_ShouldNormalizeInterpolationsAndNestings()
    {
        Placeholders.Get("{{name}} has {{ value, number(minimumFractionDigits: 2) }} {{- html}} $t(common:link, { \"a\": 1 }) $t( 'other' )")
            .ShouldBe(["$t(common:link)", "$t(other)", "{{html}}", "{{name}}", "{{value}}"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("No placeholders {single} {{ }} $t()")]
    public void Get_NoPlaceholders_ShouldReturnAnEmptySet(string text)
    {
        Placeholders.Get(text).ShouldBeEmpty();
    }

    [Fact]
    public void GetNestedKeys_ShouldReturnTheKeys()
    {
        Placeholders.GetNestedKeys("$t(a) and $t(ns:b, { \"count\": 1 })").ToList().ShouldBe(["a", "ns:b"]);
        Placeholders.GetNestedKeys(null).ShouldBeEmpty();
    }
}
