using System.IO;
using FluentAssertions;
using I18Next.Net.Backends;
using NUnit.Framework;

namespace I18Next.Net.Tests.Backends;

[TestFixture]
public class SimpleIniParserFixture
{
    private const string IniContent = "; comment\nRootKey = root value\nFlag\n\n[Section]\nKey = \"quoted value\"\nOther=value=with=equals\n";

    [Test]
    public void GetValue_RootSection_ShouldReturnValues()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetValue("RootKey").Should().Be("root value");
        parser.GetValue("rootkey").Should().Be("root value");
        parser.GetValue("Flag").Should().BeEmpty();
        parser.GetValue("Missing").Should().BeNull();
    }

    [Test]
    public void GetValue_NamedSection_ShouldReturnValues()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetValue("Section", "Key").Should().Be("quoted value");
        parser.GetValue("section", "Other").Should().Be("value=with=equals");
        parser.GetValue("Section", "Missing").Should().BeNull();
        parser.GetValue("Missing", "Key", "default").Should().Be("default");
    }

    [Test]
    public void GetSectionsAndKeys_ShouldListContent()
    {
        var parser = new SimpleIniParser(IniContent);

        parser.GetSections().Should().Equal("Section");
        parser.GetKeys("Section").Should().Equal("Key", "Other");
        parser.GetKeys("Missing").Should().BeEmpty();
    }

    [Test]
    public void FromFile_ShouldParseFile()
    {
        var parser = SimpleIniParser.FromFile(Path.Combine("TestFiles", "en-US", "test.ini"));

        parser.GetValue("SectionB.SubSectionA", "Value1").Should().Be("Translated value 1");
    }
}
