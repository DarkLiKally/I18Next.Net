using System;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class SortCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();

    public SortCommandFixture()
    {
        _context.Write("locales/en/translation.json", """{ "b": "B", "a": { "d": "D", "c": "C" } }""");
        _context.Write("locales/de/translation.json", "{\n  \"a\": \"A\",\n  \"b\": \"B\"\n}\n");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Sort_ShouldSortAndFormatTheFiles()
    {
        (await Run()).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldBe("locales/en/translation.json: sorted.\n", StringCompareShould.IgnoreLineEndings);
        _context.Read("locales/en/translation.json").ShouldBe("{\n  \"a\": {\n    \"c\": \"C\",\n    \"d\": \"D\"\n  },\n  \"b\": \"B\"\n}\n");
        _context.Read("locales/de/translation.json").ShouldBe("{\n  \"a\": \"A\",\n  \"b\": \"B\"\n}\n");

        (await Run("--check")).ShouldBe(ExitCodes.Success);
        _context.Output.ShouldContain("All translation files are sorted.");
    }

    [Fact]
    public async Task Sort_Check_ShouldFailWithoutWriting()
    {
        (await Run("--check")).ShouldBe(ExitCodes.ProblemsFound);

        _context.Output.ShouldContain("locales/en/translation.json: not sorted or formatted.");
        _context.Read("locales/en/translation.json").ShouldBe("""{ "b": "B", "a": { "d": "D", "c": "C" } }""");
    }

    [Fact]
    public async Task Sort_LanguageFilter_ShouldOnlySortTheseLanguages()
    {
        (await Run("--languages", "de")).ShouldBe(ExitCodes.Success);

        _context.Read("locales/en/translation.json").ShouldBe("""{ "b": "B", "a": { "d": "D", "c": "C" } }""");
    }

    [Fact]
    public async Task Sort_InvalidFile_ShouldFail()
    {
        _context.Write("locales/de/broken.json", "[1, 2]");

        (await Run()).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("broken.json is not a valid translation file: The root of the JSON is not an object.");
    }

    private Task<int> Run(params string[] args)
    {
        return _context.RunAsync(["sort", "-p", _context.Locales, .. args]);
    }
}
