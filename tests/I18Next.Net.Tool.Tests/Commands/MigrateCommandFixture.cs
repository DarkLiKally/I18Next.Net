using System;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class MigrateCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();

    public MigrateCommandFixture()
    {
        _context.Write("locales/en/translation.json", """
                                                      {
                                                          "item": "{{count}} item",
                                                          "item_plural": "{{count}} items",
                                                          "title": "Title"
                                                      }
                                                      """);
        _context.Write("locales/ru/translation.json", """
                                                      {
                                                          "item_0": "{{count}} предмет",
                                                          "item_1": "{{count}} предмета",
                                                          "item_2": "{{count}} предметов"
                                                      }
                                                      """);
        _context.Write("locales/de/translation.json", """
                                                      {
                                                          "title": "Titel"
                                                      }
                                                      """);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Migrate_ShouldRenameThePluralKeys()
    {
        (await Run()).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldBe("""
                                 locales/en/translation.json: renamed 2 keys.
                                 locales/ru/translation.json: renamed 3 keys.

                                 """, StringCompareShould.IgnoreLineEndings);
        _context.Read("locales/en/translation.json").ShouldBe("{\n  \"item_one\": \"{{count}} item\",\n  \"item_other\": \"{{count}} items\",\n  \"title\": \"Title\"\n}\n");
        _context.Read("locales/ru/translation.json").ShouldContain("\"item_many\": \"{{count}} предметов\"");
        _context.Read("locales/de/translation.json").ShouldContain("    \"title\"");

        (await Run("--check")).ShouldBe(ExitCodes.Success);
        _context.Output.ShouldContain("No v3 plurals found.");
    }

    [Fact]
    public async Task Migrate_Check_ShouldFailWithoutWriting()
    {
        (await Run("--dry-run")).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("locales/ru/translation.json: would rename 3 keys.");
        _context.Read("locales/ru/translation.json").ShouldContain("item_0");
    }

    [Fact]
    public async Task Migrate_Conflicts_ShouldFail()
    {
        _context.Write("locales/de/translation.json", """{ "item": "1", "item_plural": "n", "item_one": "exists" }""");

        (await Run("-l", "de")).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("item cannot be renamed to item_one, the key exists.");
        _context.Read("locales/de/translation.json").ShouldContain("\"item_other\": \"n\"");
    }

    private Task<int> Run(params string[] args)
    {
        return _context.RunAsync(["migrate", "-p", _context.Locales, .. args]);
    }
}
