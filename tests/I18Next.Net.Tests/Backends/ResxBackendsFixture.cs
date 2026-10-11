using System;
using System.IO;
using System.Resources;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class ResxBackendsFixture
{
    private static readonly ResourceManager Strings = new("I18Next.Net.Tests.Resources.Strings", typeof(ResxBackendsFixture).Assembly);

    private readonly ResxFileBackend _fileBackend = new(Path.Combine("TestFiles", "resx"));

    [Fact]
    public async Task ResxFileBackend_NeutralFile_ShouldBeUsedForNeutralLanguage()
    {
        var tree = await _fileBackend.LoadNamespaceAsync("en-US", "Strings");

        tree.Namespace.ShouldBe("Strings");
        tree.GetValue("greeting", null).ShouldBe("Hello {{name}}!");
        tree.GetValue("menu.title", null).ShouldBe("Menu");
        tree.GetValue("multiline", null).ShouldBe("First line\nSecond line");
        tree.GetValue("logo", null).ShouldBeNull();
        tree.GetValue("icon", null).ShouldBeNull();
    }

    [Fact]
    public async Task ResxFileBackend_LanguageFiles_ShouldBeFoundWithBothLayouts()
    {
        (await _fileBackend.LoadNamespaceAsync("de-AT", "Strings")).GetValue("greeting", null).ShouldBe("Servus {{name}}!");
        (await _fileBackend.LoadNamespaceAsync("de-CH", "Strings")).GetValue("menu.title", null).ShouldBe("Menü");
        (await _fileBackend.LoadNamespaceAsync("de", "common")).GetValue("save", null).ShouldBe("Speichern");
        (await _fileBackend.LoadNamespaceAsync("fr-FR", "common")).GetValue("save", null).ShouldBe("Enregistrer");
        (await _fileBackend.LoadNamespaceAsync("fr", "Strings")).ShouldBeNull();
        (await _fileBackend.LoadNamespaceAsync("en", "common")).ShouldBeNull();
    }

    [Fact]
    public async Task ResxFileBackend_OtherNeutralLanguage_ShouldBeUsed()
    {
        var backend = new ResxFileBackend(Path.Combine("TestFiles", "resx"), new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>())
        {
            NeutralLanguage = "fr"
        };

        (await backend.LoadNamespaceAsync("en", "Strings")).ShouldBeNull();
        (await backend.LoadNamespaceAsync("fr", "Strings")).ShouldBeOfType<DictionaryTranslationTree>().GetValue("menu.title", null).ShouldBe("Menu");
        new ResxFileBackend().ShouldNotBeNull();
        new ResxFileBackend(new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>()).ShouldNotBeNull();
    }

    [Fact]
    public async Task ResxFileBackend_WithI18Next_ShouldTranslate()
    {
        var i18Next = new I18NextNet(_fileBackend, new DefaultTranslator(_fileBackend)) { Language = "de", DefaultNamespace = "Strings" };
        i18Next.SetFallbackLanguages("en");

        i18Next.T("greeting", new { name = "Anna" }).ShouldBe("Hallo Anna!");
        i18Next.T("multiline").ShouldBe("First line\nSecond line");
        (await i18Next.Ta("common:save")).ShouldBe("Speichern");
    }

    [Fact]
    public void ResxTranslationReader_ShouldReadStringsAndStreams()
    {
        var content = File.ReadAllText(Path.Combine("TestFiles", "resx", "Strings.de.resx"));

        ResxTranslationReader.Read(content, "ns").GetValue("menu.title", null).ShouldBe("Menü");

        using var stream = File.OpenRead(Path.Combine("TestFiles", "resx", "Strings.de.resx"));
        ResxTranslationReader.Read(stream, new FlatTranslationTreeBuilder()).GetValue("greeting", null).ShouldBe("Hallo {{name}}!");

        ResxTranslationReader.Read("""<root><data name=""><value>x</value></data><data name="empty" /></root>""", "ns").GetValue("empty", null).ShouldBeNull();
        Should.Throw<System.Xml.XmlException>(() => ResxTranslationReader.Read("""<!DOCTYPE root [<!ENTITY x "y">]><root />""", "ns"));
    }

    [Fact]
    public async Task ResourceManagerBackend_ShouldLoadNeutralAndSatelliteResources()
    {
        var backend = new ResourceManagerBackend(Strings);

        var neutral = await backend.LoadNamespaceAsync("en-GB", "translation");
        neutral.Namespace.ShouldBe("translation");
        neutral.GetValue("greeting", null).ShouldBe("Hello {{name}}!");
        neutral.GetValue("menu.title", null).ShouldBe("Menu");

        (await backend.LoadNamespaceAsync("de-AT", "translation")).GetValue("menu.title", null).ShouldBe("Menü");
        (await backend.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
        (await backend.LoadNamespaceAsync("not a culture", "translation")).ShouldBeNull();
        (await backend.LoadNamespaceAsync("en", "other")).ShouldBeNull();
    }

    [Fact]
    public async Task ResourceManagerBackend_Namespaces_ShouldUseTheirResourceManagers()
    {
        var backend = new ResourceManagerBackend(new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>())
            .Add("strings", Strings)
            .Add("missing", new ResourceManager("I18Next.Net.Tests.Resources.Missing", typeof(ResxBackendsFixture).Assembly));
        backend.NeutralLanguage = "de";

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en", DefaultNamespace = "strings" };

        i18Next.T("de", "greeting", new { name = "Anna" }).ShouldBe("Hello Anna!");
        (await backend.LoadNamespaceAsync("de", "strings")).ShouldBeOfType<DictionaryTranslationTree>().GetValue("greeting", null).ShouldBe("Hello {{name}}!");
        (await backend.LoadNamespaceAsync("de", "missing")).ShouldBeNull();

        Should.Throw<ArgumentException>(() => backend.Add("", Strings));
        Should.Throw<ArgumentNullException>(() => backend.Add("ns", null));
        Should.Throw<ArgumentNullException>(() => new ResourceManagerBackend((ITranslationTreeBuilderFactory)null));
    }
}
