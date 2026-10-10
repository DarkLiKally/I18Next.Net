using System;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class ConvertCommandFixture : IDisposable
{
    private const string Resx = """
                                <?xml version="1.0" encoding="utf-8"?>
                                <root>
                                  <data name="Home.Title" xml:space="preserve"><value>Welcome &amp; hello</value></data>
                                  <data name="Home.Body" xml:space="preserve"><value>Text {{name}}</value><comment>Comment</comment></data>
                                  <data name="Plain" xml:space="preserve"><value>Plain</value></data>
                                  <data name="Empty"><value /></data>
                                  <data name="Icon" type="System.Resources.ResXFileRef, System.Windows.Forms"><value>icon.png</value></data>
                                </root>
                                """;

    private readonly ToolTestContext _context = new();

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Convert_ResxToJson_ShouldNestDottedKeys()
    {
        _context.Write("app.resx", Resx);

        (await Run("app.resx", "out/app.json")).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldContain("Converted");
        _context.Read("out/app.json").ShouldBe("""
                                               {
                                                 "Home": {
                                                   "Title": "Welcome & hello",
                                                   "Body": "Text {{name}}"
                                                 },
                                                 "Plain": "Plain",
                                                 "Empty": ""
                                               }

                                               """.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task Convert_JsonToResx_ShouldWriteFlatKeys()
    {
        _context.Write("app.json", """{ "Home": { "Title": "A & B <b>" }, "List": [ "x" ] }""");

        (await Run("app.json", "app.resx")).ShouldBe(ExitCodes.Success);

        var resx = _context.Read("app.resx");
        resx.ShouldStartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n  <resheader name=\"resmimetype\">");
        resx.ShouldContain("  <data name=\"Home.Title\" xml:space=\"preserve\">\n    <value>A &amp; B &lt;b&gt;</value>\n  </data>");
        resx.ShouldContain("<data name=\"List.0\" xml:space=\"preserve\">");

        (await Run("app.resx", "back.json")).ShouldBe(ExitCodes.Success);
        _context.Read("back.json").ShouldBe("{\n  \"Home\": {\n    \"Title\": \"A & B <b>\"\n  },\n  \"List\": {\n    \"0\": \"x\"\n  }\n}\n");
    }

    [Fact]
    public async Task Convert_JsonToYamlAndBack_ShouldKeepTheValues()
    {
        const string json = """
                            {
                              "greeting": "Hello {{name}}",
                              "menu": {
                                "items": [
                                  "Home",
                                  "About"
                                ]
                              },
                              "flag": "true",
                              "number": "42",
                              "empty": "",
                              "nullText": "null",
                              "multiline": "a\nb",
                              "colon": "it's: here"
                            }

                            """;
        _context.Write("en.json", json.Replace("\r\n", "\n"));

        (await Run("en.json", "en.yaml")).ShouldBe(ExitCodes.Success);
        (await Run("en.yaml", "en2.json")).ShouldBe(ExitCodes.Success);

        _context.Read("en.yaml").ShouldStartWith("greeting: Hello {{name}}\nmenu:\n  items:\n  - Home\n  - About\nflag: \"true\"\n");
        _context.Read("en2.json").ShouldBe(json.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task Convert_YamlWithNullsAndNonScalarKeys_ShouldSkipThem()
    {
        _context.Write("en.yml", "a: ~\nb:\nc: null\nd: 'null'\n? [x]\n: y\ne:\n  - 1\n  - ~\n");

        (await Run("en.yml", "en.json")).ShouldBe(ExitCodes.Success);

        _context.Read("en.json").ShouldBe("{\n  \"d\": \"null\",\n  \"e\": [\n    \"1\",\n    null\n  ]\n}\n");
    }

    [Fact]
    public async Task Convert_FlatAndNested_ShouldRestructureTheKeys()
    {
        _context.Write("nested.json", """{ "a": { "b": "1", "c": { "d": "2" } }, "e": "3" }""");

        (await Run("nested.json", "flat.json", "--flat")).ShouldBe(ExitCodes.Success);
        _context.Read("flat.json").ShouldBe("{\n  \"a.b\": \"1\",\n  \"a.c.d\": \"2\",\n  \"e\": \"3\"\n}\n");

        (await Run("flat.json", "nested2.json", "--nested")).ShouldBe(ExitCodes.Success);
        _context.Read("nested2.json").ShouldBe("{\n  \"a\": {\n    \"b\": \"1\",\n    \"c\": {\n      \"d\": \"2\"\n    }\n  },\n  \"e\": \"3\"\n}\n");

        (await Run("flat.json", "colon.json", "--nested", "--key-separator", "_")).ShouldBe(ExitCodes.Success);
        _context.Read("colon.json").ShouldContain("\"a.b\": \"1\"");
    }

    [Fact]
    public async Task Convert_Directory_ShouldConvertEveryFile()
    {
        _context.Write("locales/en/translation.json", """{ "a": "A" }""");
        _context.Write("locales/de/translation.json", """{ "a": "A" }""");
        _context.Write("locales/de/common.yaml", "b: B\n");
        _context.Write("locales/readme.txt", "ignored");

        (await Run("locales", "yaml", "--to", "yaml")).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldContain("Converted 2 files");
        _context.Read("yaml/en/translation.yaml").ShouldBe("a: A\n");
        _context.Read("yaml/de/translation.yaml").ShouldBe("a: A\n");
        _context.Exists("yaml/de/common.yaml").ShouldBeFalse();

        (await Run("locales", "resx", "--from", "yaml", "--to", "resx")).ShouldBe(ExitCodes.Success);
        _context.Exists("resx/de/common.resx").ShouldBeTrue();
        _context.Exists("resx/de/translation.resx").ShouldBeFalse();
    }

    [Fact]
    public async Task Convert_ExplicitFormats_ShouldOverrideTheExtensions()
    {
        _context.Write("strings.txt", "a: A\n");

        (await Run("strings.txt", "strings.out", "--from", "yaml", "--to", "json")).ShouldBe(ExitCodes.Success);

        _context.Read("strings.out").ShouldBe("{\n  \"a\": \"A\"\n}\n");
    }

    [Theory]
    [InlineData("missing.json", "out.json", null, "does not exist")]
    [InlineData("in.json", "out.txt", null, "cannot be detected from its extension")]
    [InlineData("in.json", "out.json", "--flat --nested", "--flat and --nested cannot be combined.")]
    [InlineData("dir", "out", null, "--to is required to convert a directory.")]
    [InlineData("broken.yaml", "out.json", null, "is not a valid yaml file")]
    [InlineData("broken.resx", "out.json", null, "is not a valid resx file")]
    [InlineData("broken.json", "out.yaml", null, "is not a valid json file")]
    public async Task Convert_InvalidInput_ShouldFail(string input, string output, string options, string error)
    {
        _context.Write("in.json", "{}");
        _context.Write("dir/in.json", "{}");
        _context.Write("broken.yaml", "a: [");
        _context.Write("broken.resx", "<root>");
        _context.Write("broken.json", "{");

        (await Run([input, output, .. options?.Split(' ') ?? []])).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain(error);
    }

    private Task<int> Run(params string[] args)
    {
        var paths = new[] { _context.GetPath(args[0]), _context.GetPath(args[1]) };

        return _context.RunAsync(["convert", .. paths, .. args[2..]]);
    }
}
