using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests;

[TestFixture]
public class I18NextObjectsFixture
{
    private I18NextNet _i18Next;
    private List<string> _missingKeys;

    [SetUp]
    public void SetUp()
    {
        var backend = new JsonFileBackend(Path.Combine("TestFiles", "objects"));
        var translator = new DefaultTranslator(backend);

        _missingKeys = new List<string>();
        translator.MissingKey += (_, args) => _missingKeys.Add(args.Key);

        _i18Next = new I18NextNet(backend, translator) { Language = "en" };
    }

    [Test]
    public void TObject_Group_ShouldReturnTranslatedNestedValues()
    {
        var menu = _i18Next.TObject("menu", new { name = "Stefan", year = 2026 });

        menu["title"].Should().Be("Hello Stefan");
        menu["items"].Should().BeEquivalentTo(new object[] { "Home", "About Stefan", "Contact" });
        menu["count"].Should().Be("3");

        var footer = (IDictionary<string, object>) menu["footer"];
        footer["copyright"].Should().Be("© 2026");

        var links = (object[]) footer["links"];
        links.Should().HaveCount(2);
        ((IDictionary<string, object>) links[1])["label"].Should().Be("Imprint");
    }

    [Test]
    public void TObject_NestedKey_ShouldReturnSubGroup()
    {
        var footer = _i18Next.TObject("menu.footer", new { year = 2026 });

        footer.Keys.Should().BeEquivalentTo("copyright", "links");
    }

    [Test]
    public async Task TaObject_OtherLanguage_ShouldUseLanguage()
    {
        var menu = await _i18Next.TaObject("de", "menu", new { name = "Stefan" });

        menu["title"].Should().Be("Hallo Stefan");
        menu["items"].Should().BeEquivalentTo(new object[] { "Start", "Über Stefan" });
    }

    [Test]
    public void TObject_FallbackLanguage_ShouldBeUsedForMissingGroups()
    {
        _i18Next.SetFallbackLanguages("en");

        _i18Next.TObject("de", "onlyEnglish")["value"].Should().Be("English fallback");
    }

    [Test]
    public void TObject_GroupExistsInLanguage_ShouldNotMergeFallbackValues()
    {
        _i18Next.SetFallbackLanguages("en");

        _i18Next.TObject("de", "menu").Should().NotContainKey("footer");
    }

    [Test]
    public void TObject_MissingOrSingleValueKey_ShouldReturnNullAndRaiseMissingKey()
    {
        _i18Next.TObject("missing").Should().BeNull();
        _i18Next.TObject("menu.title").Should().BeNull();

        _missingKeys.Should().Equal("missing", "menu.title");
    }

    [Test]
    public void TObject_Namespace_ShouldBeSupported()
    {
        _i18Next.TObject("translation:list").Should().ContainKeys("0", "1", "2");
    }

    [Test]
    public void TModel_ClassMapping_ShouldMapNestedValues()
    {
        var menu = _i18Next.T<Menu>("menu", new { name = "Stefan", year = 2026 });

        menu.Title.Should().Be("Hello Stefan");
        menu.Items.Should().Equal("Home", "About Stefan", "Contact");
        menu.Count.Should().Be(3);
        menu.Footer.Copyright.Should().Be("© 2026");
        menu.Footer.Links.Should().HaveCount(2);
        menu.Footer.Links[0].Url.Should().Be("/privacy");
    }

    [Test]
    public async Task TaModel_Array_ShouldMapToArray()
    {
        (await _i18Next.Ta<string[]>("list")).Should().Equal("first", "second", "third");
        _i18Next.T<List<string>>("de", "menu.items", new { name = "Stefan" }).Should().Equal("Start", "Über Stefan");
    }

    [Test]
    public void TModel_Missing_ShouldReturnDefault()
    {
        _i18Next.T<Menu>("missing").Should().BeNull();
        (_i18Next.Ta<Menu>("de", "missing").Result).Should().BeNull();
    }

    [Test]
    public void T_JoinArrays_ShouldJoinTranslatedItems()
    {
        _i18Next.T("menu.items", new { joinArrays = ", ", name = "Stefan" }).Should().Be("Home, About Stefan, Contact");
        _i18Next.T("list", new { joinArrays = "\n" }).Should().Be("first\nsecond\nthird");
    }

    [Test]
    public void T_GroupKey_ShouldReturnObjectMessageInsteadOfThrowing()
    {
        _i18Next.T("menu").Should().Be("key 'translation:menu (en)' returned an object instead of string.");
        _i18Next.T("menu.footer.links", new { joinArrays = "+" }).Should().Be("key 'translation:menu.footer.links (en)' returned an object instead of string.");
        _missingKeys.Should().BeEmpty();
    }

    [Test]
    public void T_ArrayItems_ShouldBeAccessibleByIndex()
    {
        _i18Next.T("menu.items.1", new { name = "Stefan" }).Should().Be("About Stefan");
        _i18Next.T("menu.footer.links.0.label").Should().Be("Privacy");
    }

    [Test]
    public void TObject_InMemoryBackend_ShouldUseFlatKeys()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "group.a", "A {{value}}");
        backend.AddTranslation("en", "translation", "group.b.c", "C");
        backend.AddTranslation("en", "translation", "array.0", "zero");
        backend.AddTranslation("en", "translation", "array.1", "one");

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

        var group = i18Next.TObject("group", new { value = 1 });
        group["a"].Should().Be("A 1");
        ((IDictionary<string, object>) group["b"])["c"].Should().Be("C");
        i18Next.T<string[]>("array").Should().Equal("zero", "one");
        i18Next.T("array", new { joinArrays = "-" }).Should().Be("zero-one");
        i18Next.TObject("missing").Should().BeNull();
    }

    private class Menu
    {
        public string Title { get; set; }

        public List<string> Items { get; set; }

        public int Count { get; set; }

        public Footer Footer { get; set; }
    }

    private class Footer
    {
        public string Copyright { get; set; }

        public Link[] Links { get; set; }
    }

    private class Link
    {
        public string Label { get; set; }

        public string Url { get; set; }
    }
}
