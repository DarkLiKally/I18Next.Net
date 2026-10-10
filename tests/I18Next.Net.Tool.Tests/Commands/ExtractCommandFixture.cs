using System;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class ExtractCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();

    public ExtractCommandFixture()
    {
        _context.Write("src/Home.cs", """
                                      public class Home(II18Next i18n, IStringLocalizer<Home> localizer)
                                      {
                                          public void Run(int n)
                                          {
                                              i18n.T("greeting", new { name = "x" });
                                              i18n.T("menu.title");
                                              i18n.T("items", new { count = n });
                                              i18n.T("common:save");
                                              _ = localizer["welcome"];
                                              i18n.TObject("group");
                                          }
                                      }
                                      """);
        _context.Write("src/Views/Index.cshtml", """<h1>@I18n.T("view.title")</h1>""");
        _context.Write("src/bin/Generated.cs", """i18n.T("ignored");""");
        _context.Write("locales/en/translation.json", """
                                                      {
                                                        "greeting": "Hello {{name}}",
                                                        "unused": "Unused",
                                                        "nested": "$t(referenced)",
                                                        "referenced": "Referenced"
                                                      }
                                                      """);
        _context.Write("locales/ru/translation.json", "{}\n");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Extract_ShouldAddTheMissingKeysToAllLanguages()
    {
        var exitCode = await Run();

        exitCode.ShouldBe(ExitCodes.Success);
        _context.Output.ShouldContain("Found 7 keys in 2 files.");
        _context.Output.ShouldContain("locales/en/translation.json: added 5 and removed 0 keys.");
        _context.Read("locales/en/translation.json").ShouldBe("""
                                                             {
                                                               "greeting": "Hello {{name}}",
                                                               "unused": "Unused",
                                                               "nested": "$t(referenced)",
                                                               "referenced": "Referenced",
                                                               "menu": {
                                                                 "title": ""
                                                               },
                                                               "items_one": "",
                                                               "items_other": "",
                                                               "welcome": "",
                                                               "view": {
                                                                 "title": ""
                                                               }
                                                             }

                                                             """.Replace("\r\n", "\n"));
        _context.Read("locales/ru/translation.json").ShouldContain("  \"items_one\": \"\",\n  \"items_few\": \"\",\n  \"items_many\": \"\",\n  \"items_other\": \"\",\n");
        _context.Read("locales/ru/common.json").ShouldBe("{\n  \"save\": \"\"\n}\n");
        _context.Read("locales/en/translation.json").ShouldNotContain("group");
        _context.Read("locales/en/translation.json").ShouldNotContain("ignored");
    }

    [Fact]
    public async Task Extract_KeyAsDefaultValue_ShouldUseTheKey()
    {
        (await Run("--default-value", "key", "--languages", "en")).ShouldBe(ExitCodes.Success);

        _context.Read("locales/en/translation.json").ShouldContain("\"items_one\": \"items_one\"");
        _context.Read("locales/en/common.json").ShouldContain("\"save\": \"save\"");
        _context.Exists("locales/ru/common.json").ShouldBeFalse();
    }

    [Fact]
    public async Task Extract_Check_ShouldFailWithoutWritingWhenFilesWouldChange()
    {
        var original = _context.Read("locales/en/translation.json");

        (await Run("--check")).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("locales/en/translation.json: would add 5 and remove 0 keys.");
        _context.Read("locales/en/translation.json").ShouldBe(original);
        _context.Exists("locales/ru/common.json").ShouldBeFalse();

        (await Run()).ShouldBe(ExitCodes.Success);
        (await Run("--dry-run")).ShouldBe(ExitCodes.Success);
        _context.Output.ShouldContain("The translation files are up to date.");
    }

    [Fact]
    public async Task Extract_RemoveUnused_ShouldKeepUsedAndNestedKeys()
    {
        (await Run("--remove-unused", "--languages", "en")).ShouldBe(ExitCodes.Success);

        var json = _context.Read("locales/en/translation.json");
        json.ShouldNotContain("unused");
        json.ShouldNotContain("nested");
        json.ShouldContain("\"greeting\": \"Hello {{name}}\"");
        json.ShouldContain("\"referenced\": \"Referenced\"");
        _context.Output.ShouldContain("locales/en/translation.json: added 5 and removed 2 keys.");
    }

    [Fact]
    public async Task Extract_NamespaceFilterAndFlatKeys_ShouldBeApplied()
    {
        (await Run("--namespaces", "translation", "--key-separator", "false", "-l", "en")).ShouldBe(ExitCodes.Success);

        _context.Read("locales/en/translation.json").ShouldContain("\"menu.title\": \"\"");
        _context.Exists("locales/en/common.json").ShouldBeFalse();
    }

    [Fact]
    public async Task Extract_NoLanguages_ShouldFail()
    {
        (await _context.RunAsync("extract", "-s", _context.GetPath("src"), "-p", _context.GetPath("missing"))).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("No language directories found");
    }

    [Fact]
    public async Task Extract_MissingSource_ShouldFail()
    {
        (await _context.RunAsync("extract", "-s", _context.GetPath("nothing"), "-p", _context.Locales)).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("does not exist");
    }

    [Fact]
    public async Task Extract_SingleFileAndCustomFunction_ShouldBeScanned()
    {
        _context.Write("Other.cs", """helper.Translate("custom.key");""");

        (await _context.RunAsync("extract", "-s", _context.GetPath("Other.cs"), "-p", _context.Locales, "-l", "en", "--functions", "Translate"))
            .ShouldBe(ExitCodes.Success);

        _context.Read("locales/en/translation.json").ShouldContain("\"custom\": {");
    }

    [Fact]
    public async Task Extract_BlockedKey_ShouldBeReported()
    {
        _context.Write("Other.cs", """i18n.T("greeting.child"); i18n.T("list.5");""");
        _context.Write("locales/en/translation.json", """{ "greeting": "Hello", "list": [ "a" ] }""");

        (await _context.RunAsync("extract", "-s", _context.GetPath("Other.cs"), "-p", _context.Locales, "-l", "en")).ShouldBe(ExitCodes.Success);

        _context.Read("locales/en/translation.json").ShouldContain("\"greeting.child\": \"\"");
        _context.Output.ShouldContain("cannot add list.5");
    }

    private Task<int> Run(params string[] args)
    {
        return _context.RunAsync(["extract", "-s", _context.GetPath("src"), "-p", _context.Locales, .. args]);
    }
}
