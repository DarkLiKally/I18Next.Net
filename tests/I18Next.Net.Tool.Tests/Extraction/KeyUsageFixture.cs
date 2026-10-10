using I18Next.Net.Tool.Extraction;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Extraction;

public class KeyUsageFixture
{
    [Fact]
    public void IsUsed_ShouldMatchKeysVariantsAndChildren()
    {
        var usage = new KeyUsage(".", ":");
        usage.AddRange([
            new ExtractedKey("translation", "item", "File.cs", 1),
            new ExtractedKey("translation", "menu", "File.cs", 2) { ReturnsObject = true },
            new ExtractedKey("common", "save", "File.cs", 3)
        ]);

        usage.IsUsed("translation", "item").ShouldBeTrue();
        usage.IsUsed("translation", "item_one").ShouldBeTrue();
        usage.IsUsed("translation", "item_male_other").ShouldBeTrue();
        usage.IsUsed("translation", "menu.items.0").ShouldBeTrue();
        usage.IsUsed("common", "save").ShouldBeTrue();

        usage.IsUsed("translation", "items").ShouldBeFalse();
        usage.IsUsed("translation", "save").ShouldBeFalse();
        usage.IsUsed("common", "item").ShouldBeFalse();
        usage.IsUsed("translation", "other").ShouldBeFalse();
    }

    [Fact]
    public void AddNestings_ShouldMarkNestedKeysAsUsed()
    {
        var usage = new KeyUsage("", "::");

        usage.AddNestings("translation", "$t(appName) and $t(common::save, { \"a\": 1 })");

        usage.IsUsed("translation", "appName").ShouldBeTrue();
        usage.IsUsed("common", "save").ShouldBeTrue();
        usage.IsUsed("common", "save.child").ShouldBeFalse();
        usage.IsUsed("translation", "save").ShouldBeFalse();
    }
}
