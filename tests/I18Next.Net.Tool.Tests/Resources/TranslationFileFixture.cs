using System.IO;
using System.Text;

using I18Next.Net.Tool.Resources;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Resources;

public class TranslationFileFixture
{
    [Fact]
    public void Save_CrlfFile_ShouldKeepTheLineEndings()
    {
        using var context = new ToolTestContext();
        var path = context.Write("en/translation.json", "{\r\n  \"a\": \"1\"\r\n}\r\n");

        var file = TranslationFile.Load(path, "en", "translation", ".");
        file.Save(false).ShouldBeFalse();

        file.Document.SetValue("b", "2");
        file.Save(false).ShouldBeTrue();

        File.ReadAllText(path).ShouldBe("{\r\n  \"a\": \"1\",\r\n  \"b\": \"2\"\r\n}\r\n");
    }

    [Fact]
    public void Save_FileWithBom_ShouldBeWrittenWithoutBom()
    {
        using var context = new ToolTestContext();
        var path = context.GetPath("en/translation.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\n  \"a\": \"1\"\n}\n", new UTF8Encoding(true));

        var file = TranslationFile.Load(path, "en", "translation", ".");

        file.Document.GetValue("a").ShouldBe("1");
        file.Save(true).ShouldBeTrue();
        File.ReadAllBytes(path)[0].ShouldBe((byte)0xEF);

        file.Save(false).ShouldBeTrue();
        File.ReadAllBytes(path)[0].ShouldBe((byte)'{');
    }

    [Fact]
    public void Save_MissingFile_ShouldOnlyBeCreatedWithContent()
    {
        using var context = new ToolTestContext();
        var path = context.GetPath("de/common.json");

        var file = TranslationFile.Load(path, "de", "common", ".");

        file.Exists.ShouldBeFalse();
        file.Save(false).ShouldBeFalse();
        File.Exists(path).ShouldBeFalse();

        file.Document.SetValue("a", "");
        file.Save(false).ShouldBeTrue();
        File.ReadAllText(path).ShouldBe("{\n  \"a\": \"\"\n}\n");
    }

    [Fact]
    public void Load_InvalidJson_ShouldThrowAToolException()
    {
        using var context = new ToolTestContext();
        var path = context.Write("en/translation.json", "{ broken");

        Should.Throw<ToolException>(() => TranslationFile.Load(path, "en", "translation", ".")).Message.ShouldContain("is not a valid translation file");
    }
}
