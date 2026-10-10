using System.Text.Json.Nodes;

using I18Next.Net.Tool.Resources;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Resources;

public class PluralMigrationFixture
{
    [Theory]
    [InlineData("en", """{"item":"1","item_plural":"n","friend_male":"1","friend_male_plural":"n","alone_plural":"n","title":"t","step_1":"s"}""",
        """{"item_one":"1","item_other":"n","friend_male_one":"1","friend_male_other":"n","alone_other":"n","title":"t","step_1":"s"}""")]
    [InlineData("fr", """{"item":"1","item_plural":"n"}""", """{"item_one":"1","item_other":"n"}""")]
    [InlineData("ru", """{"item_0":"1","item_1":"2","item_2":"5","step_1":"s"}""", """{"item_one":"1","item_few":"2","item_many":"5","step_1":"s"}""")]
    [InlineData("ar", """{"item_0":"0","item_1":"1","item_2":"2","item_3":"3","item_4":"11","item_5":"100"}""",
        """{"item_zero":"0","item_one":"1","item_two":"2","item_few":"3","item_many":"11","item_other":"100"}""")]
    [InlineData("ja", """{"item_0":"n"}""", """{"item_other":"n"}""")]
    [InlineData("xx", """{"item":"1","item_plural":"n","item_0":"0"}""", """{"item":"1","item_plural":"n","item_0":"0"}""")]
    public void Migrate_ShouldRenameVersion3Plurals(string language, string json, string expected)
    {
        var root = JsonNode.Parse(json)!.AsObject();

        new PluralMigration(language).Migrate(root);

        root.ToJsonString().ShouldBe(expected);
    }

    [Fact]
    public void Migrate_NestedObjects_ShouldBeMigratedAndCounted()
    {
        var root = JsonNode.Parse("""{"cart":{"item":"1","item_plural":"n","deep":{"x":"1","x_plural":"n"}},"other":"o"}""")!.AsObject();

        new PluralMigration("de").Migrate(root).ShouldBe(4);

        root.ToJsonString().ShouldBe("""{"cart":{"item_one":"1","item_other":"n","deep":{"x_one":"1","x_other":"n"}},"other":"o"}""");
    }

    [Fact]
    public void Migrate_ExistingTargetKey_ShouldReportAConflict()
    {
        var root = JsonNode.Parse("""{"menu":{"item":"1","item_plural":"n","item_other":"exists"}}""")!.AsObject();
        var migration = new PluralMigration("en");

        migration.Migrate(root).ShouldBe(1);

        migration.Conflicts.ShouldBe(["menu.item_plural cannot be renamed to item_other, the key exists."]);
        root.ToJsonString().ShouldBe("""{"menu":{"item_one":"1","item_plural":"n","item_other":"exists"}}""");
    }

    [Fact]
    public void Migrate_Version4File_ShouldNotChange()
    {
        var root = JsonNode.Parse("""{"item_one":"1","item_other":"n","group":{"a":"b"}}""")!.AsObject();

        new PluralMigration("en").Migrate(root).ShouldBe(0);
        new PluralMigration("ru").Migrate(root).ShouldBe(0);
    }
}
