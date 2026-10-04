using System.IO;

using I18Next.Net.Backends;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class SimpleIniParserFixture
{
    private const string IniContent = "; comment\nRootKey = root value\nFlag\n\n[Section]\nKey = \"quoted value\"\nOther=value=with=equals\n";

    [Fact]
    public void GetValue_RootSection_ShouldReturnValues()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetValue("RootKey").ShouldBe("root value");
        parser.GetValue("rootkey").ShouldBe("root value");
        parser.GetValue("Flag").ShouldBeEmpty();
        parser.GetValue("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetValue_NamedSection_ShouldReturnValues()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetValue("Section", "Key").ShouldBe("quoted value");
        parser.GetValue("section", "Other").ShouldBe("value=with=equals");
        parser.GetValue("Section", "Missing").ShouldBeNull();
        parser.GetValue("Missing", "Key", "default").ShouldBe("default");
    }

    [Fact]
    public void GetSectionsAndKeys_ShouldListContent()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetSections().ShouldBe(["Section"]);
        parser.GetKeys("Section").ShouldBe(["Key", "Other"]);
        parser.GetKeys("Missing").ShouldBeEmpty();
    }

    [Fact]
    public void FromFile_ShouldParseFile()
    {
        var parser = SimpleIniParser.FromFile(Path.Combine("TestFiles", "en-US", "test.ini"));

        parser.GetValue("SectionB.SubSectionA", "Value1").ShouldBe("Translated value 1");
    }
}
