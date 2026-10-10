using System;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class CheckCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Check_CompleteTranslations_ShouldSucceed()
    {
        _context.Write("locales/en/translation.json", """{ "greeting": "Hello {{name}}", "item_one": "One item", "item_other": "{{count}} items" }""");
        _context.Write("locales/de/translation.json", """{ "greeting": "Hallo {{ name }}", "item_one": "{{count}} Artikel", "item_other": "{{count}} Artikel" }""");
        _context.Write("locales/ja/translation.json", """{ "greeting": "こんにちは {{name}}", "item_other": "{{count}} 個" }""");

        (await Run()).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldBe("Comparing de, ja with en.\nNo problems found.\n", StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public async Task Check_Problems_ShouldBeReportedAndFail()
    {
        _context.Write("locales/en/translation.json", """
                                                      {
                                                        "greeting": "Hello {{name}}",
                                                        "farewell": "Bye $t(appName)",
                                                        "item_one": "One item",
                                                        "item_other": "{{count}} items",
                                                        "place_ordinal_one": "{{count}}st",
                                                        "place_ordinal_other": "{{count}}th",
                                                        "todo": ""
                                                      }
                                                      """);
        _context.Write("locales/en/common.json", """{ "save": "Save" }""");
        _context.Write("locales/ru/translation.json", """
                                                      {
                                                        "greeting": "Привет {{nmae}}",
                                                        "farewell": "Пока",
                                                        "item_one": "{{count}} предмет",
                                                        "item_few": "",
                                                        "place_ordinal_other": "{{count}}-й"
                                                      }
                                                      """);

        (await Run()).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldBe("""
                                 Comparing ru with en.
                                 Missing translations (4):
                                   ru  common:save
                                   ru  translation:item_many
                                   ru  translation:item_other
                                   ru  translation:todo
                                 Empty values (2):
                                   en  translation:todo
                                   ru  translation:item_few
                                 Placeholder mismatches (2):
                                   ru  translation:greeting  missing {{name}}; unexpected {{nmae}}
                                   ru  translation:farewell  missing $t(appName)
                                 8 problems found.

                                 """, StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public async Task Check_Source_ShouldReportUnusedAndMissingKeys()
    {
        _context.Write("src/App.cs", """i18n.T("greeting"); i18n.T("items", new { count = 1 }); i18n.TObject("menu");""");
        _context.Write("locales/en/translation.json", """
                                                      { "greeting": "Hi $t(appName)", "appName": "App", "old": "Old", "menu": { "a": "A" }, "items_one": "1" }
                                                      """);

        (await Run("-s", _context.GetPath("src"))).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("""
                                      Missing translations (1):
                                        en  translation:items_other
                                      Unused keys (1):
                                        en  translation:old
                                      2 problems found.
                                      """, Case.Sensitive);
    }

    [Fact]
    public async Task Check_ReferenceLanguage_ShouldBeUsed()
    {
        _context.Write("locales/de/translation.json", """{ "a": "A", "b": "B" }""");
        _context.Write("locales/fr/translation.json", """{ "a": "A", "b": "B" }""");

        (await Run()).ShouldBe(ExitCodes.Success);
        _context.Output.ShouldContain("Comparing fr with de.");

        (await Run("--reference", "fr")).ShouldBe(ExitCodes.Success);

        (await Run("-r", "es")).ShouldBe(ExitCodes.Error);
        _context.Error.ShouldContain("The reference language es is not one of the languages de, fr.");
    }

    [Fact]
    public async Task Check_MissingDirectory_ShouldFail()
    {
        (await Run()).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("does not exist");

        System.IO.Directory.CreateDirectory(_context.Locales);
        (await Run()).ShouldBe(ExitCodes.Error);
        _context.Error.ShouldContain("No language directories found");
    }

    private Task<int> Run(params string[] args)
    {
        return _context.RunAsync(["check", "-p", _context.Locales, .. args]);
    }
}
