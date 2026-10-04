using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests;

public class I18NextObjectsFixture
{
    public I18NextObjectsFixture()
    {
        var backend = new JsonFileBackend(Path.Combine("TestFiles", "objects"));
        var translator = new DefaultTranslator(backend);

        _missingKeys = [];
        translator.MissingKey += (_, args) => _missingKeys.Add(args.Key);

        _i18Next = new I18NextNet(backend, translator) { Language = "en" };
    }
    private readonly I18NextNet _i18Next;
    private readonly List<string> _missingKeys;


    [Fact]
    public void TObject_Group_ShouldReturnTranslatedNestedValues()
    {
        var menu = _i18Next.TObject("menu", new { name = "Stefan", year = 2026 });

        menu["title"].ShouldBe("Hello Stefan");
        menu["items"].ShouldBeEquivalentTo(new object[] { "Home", "About Stefan", "Contact" });
        menu["count"].ShouldBe("3");

        var footer = (IDictionary<string, object>)menu["footer"];
        footer["copyright"].ShouldBe("© 2026");

        var links = (object[])footer["links"];
        links.Count().ShouldBe(2);
        ((IDictionary<string, object>)links[1])["label"].ShouldBe("Imprint");
    }

    [Fact]
    public void TObject_NestedKey_ShouldReturnSubGroup()
    {
        var footer = _i18Next.TObject("menu.footer", new { year = 2026 });

        footer.Keys.ShouldBe(new[] { "copyright", "links" }, true);
    }

    [Fact]
    public async Task TaObject_OtherLanguage_ShouldUseLanguage()
    {
        var menu = await _i18Next.TaObject("de", "menu", new { name = "Stefan" });

        menu["title"].ShouldBe("Hallo Stefan");
        menu["items"].ShouldBeEquivalentTo(new object[] { "Start", "Über Stefan" });
    }

    [Fact]
    public void TObject_FallbackLanguage_ShouldBeUsedForMissingGroups()
    {
        _i18Next.SetFallbackLanguages("en");

        _i18Next.TObject("de", "onlyEnglish")["value"].ShouldBe("English fallback");
    }

    [Fact]
    public void TObject_GroupExistsInLanguage_ShouldNotMergeFallbackValues()
    {
        _i18Next.SetFallbackLanguages("en");

        _i18Next.TObject("de", "menu").ShouldNotContainKey("footer");
    }

    [Fact]
    public void TObject_MissingOrSingleValueKey_ShouldReturnNullAndRaiseMissingKey()
    {
        _i18Next.TObject("missing").ShouldBeNull();
        _i18Next.TObject("menu.title").ShouldBeNull();

        _missingKeys.ShouldBe(new[] { "missing", "menu.title" });
    }

    [Fact]
    public void TObject_Namespace_ShouldBeSupported()
    {
        _i18Next.TObject("translation:list").Keys.ShouldBe(new[] { "0", "1", "2" }, true);
    }

    [Fact]
    public void TModel_ClassMapping_ShouldMapNestedValues()
    {
        var menu = _i18Next.T<Menu>("menu", new { name = "Stefan", year = 2026 });

        menu.Title.ShouldBe("Hello Stefan");
        menu.Items.ShouldBe(new[] { "Home", "About Stefan", "Contact" });
        menu.Count.ShouldBe(3);
        menu.Footer.Copyright.ShouldBe("© 2026");
        menu.Footer.Links.Count().ShouldBe(2);
        menu.Footer.Links[0].Url.ShouldBe("/privacy");
    }

    [Fact]
    public async Task TaModel_Array_ShouldMapToArray()
    {
        (await _i18Next.Ta<string[]>("list")).ShouldBe(["first", "second", "third"]);
        _i18Next.T<List<string>>("de", "menu.items", new { name = "Stefan" }).ShouldBe(new[] { "Start", "Über Stefan" });
    }

    [Fact]
    public async Task TModel_Missing_ShouldReturnDefault()
    {
        _i18Next.T<Menu>("missing").ShouldBeNull();
        (await _i18Next.Ta<Menu>("de", "missing")).ShouldBeNull();
    }

    [Fact]
    public void T_JoinArrays_ShouldJoinTranslatedItems()
    {
        _i18Next.T("menu.items", new { joinArrays = ", ", name = "Stefan" }).ShouldBe("Home, About Stefan, Contact");
        _i18Next.T("list", new { joinArrays = "\n" }).ShouldBe("first\nsecond\nthird");
    }

    [Fact]
    public void T_GroupKey_ShouldReturnObjectMessageInsteadOfThrowing()
    {
        _i18Next.T("menu").ShouldBe("key 'translation:menu (en)' returned an object instead of string.");
        _i18Next.T("menu.footer.links", new { joinArrays = "+" }).ShouldBe("key 'translation:menu.footer.links (en)' returned an object instead of string.");
        _missingKeys.ShouldBeEmpty();
    }

    [Fact]
    public void T_ArrayItems_ShouldBeAccessibleByIndex()
    {
        _i18Next.T("menu.items.1", new { name = "Stefan" }).ShouldBe("About Stefan");
        _i18Next.T("menu.footer.links.0.label").ShouldBe("Privacy");
    }

    [Fact]
    public void TObject_InMemoryBackend_ShouldUseFlatKeys()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "group.a", "A {{value}}");
        backend.AddTranslation("en", "translation", "group.b.c", "C");
        backend.AddTranslation("en", "translation", "array.0", "zero");
        backend.AddTranslation("en", "translation", "array.1", "one");

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

        var group = i18Next.TObject("group", new { value = 1 });
        group["a"].ShouldBe("A 1");
        ((IDictionary<string, object>)group["b"])["c"].ShouldBe("C");
        i18Next.T<string[]>("array").ShouldBe(["zero", "one"]);
        i18Next.T("array", new { joinArrays = "-" }).ShouldBe("zero-one");
        i18Next.TObject("missing").ShouldBeNull();
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
