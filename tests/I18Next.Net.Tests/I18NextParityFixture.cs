using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests;

public class I18NextParityFixture
{
    private readonly InMemoryBackend _backend = new();
    private readonly DefaultTranslator _translator;
    private readonly I18NextNet _i18Next;

    public I18NextParityFixture()
    {
        _backend.AddTranslation("en", "translation", "menu.title", "Menu {{name}}");
        _backend.AddTranslation("en", "translation", "menu.items.0", "Home");
        _backend.AddTranslation("en", "translation", "menu.items.1", "About");
        _backend.AddTranslation("en", "translation", "empty", "");
        _backend.AddTranslation("en", "translation", "price", "{{value, number}}");
        _backend.AddTranslation("en", "translation", "amount", "{{value, currency(EUR)}}");
        _backend.AddTranslation("en", "translation", "shout", "{{value, number, uppercase}}");
        _backend.AddTranslation("en", "translation", "custom", "{{value, N1}}");
        _backend.AddTranslation("en", "common", "menu.save", "Save");
        _backend.AddTranslation("de", "translation", "menu.title", "Menü {{name}}");
        _backend.AddTranslation("de", "translation", "empty", "Leer");
        _backend.AddTranslation("de", "common", "menu.save", "Speichern");

        _translator = new DefaultTranslator(_backend);
        _i18Next = new I18NextNet(_backend, _translator) { Language = "en" };
    }

    [Fact]
    public async Task GetFixedT_Language_ShouldTranslateInLanguage()
    {
        var t = _i18Next.GetFixedT("de");

        t.Language.ShouldBe("de");
        t.T("menu.title", new { name = "A" }).ShouldBe("Menü A");
        (await t.Ta("menu.title", new { name = "B" })).ShouldBe("Menü B");
        t.TObject("menu").ShouldContainKey("title");
        (await t.TaObject("menu")).ShouldContainKey("title");
        t.T<string[]>("menu.items").ShouldBeNull();
        (await t.Ta<Dictionary<string, string>>("menu")).ShouldContainKey("title");
        t.Exists("menu.title").ShouldBeTrue();
        t.Exists("missing").ShouldBeFalse();
    }

    [Fact]
    public async Task GetFixedT_NamespaceAndKeyPrefix_ShouldPrefixKeys()
    {
        var t = _i18Next.GetFixedT(null, "common", "menu");

        t.Namespace.ShouldBe("common");
        t.KeyPrefix.ShouldBe("menu");
        t.NamespaceSeparator.ShouldBe(":");
        t.T("save").ShouldBe("Save");
        t.T("translation:title", new { name = "X" }).ShouldBe("Menu X");
        (await t.Ta("save")).ShouldBe("Save");
        t.T<string[]>("translation:items").ShouldBe(["Home", "About"]);
        (await t.Ta<string[]>("translation:items")).ShouldBe(["Home", "About"]);
        t.TObject("translation:items").Count.ShouldBe(2);
        (await t.TaObject("translation:items")).Count.ShouldBe(2);
        t.Exists("save").ShouldBeTrue();

        _i18Next.Language = "de";
        t.T("save").ShouldBe("Speichern");
    }

    [Fact]
    public void GetFixedT_WithoutOptions_ShouldBehaveLikeT()
    {
        var t = new FixedT(_i18Next, namespaceSeparator: null);

        t.T("menu.title", new { name = "A" }).ShouldBe("Menu A");
        Should.Throw<ArgumentNullException>(() => new FixedT(null));
    }

    [Fact]
    public async Task KeyPrefix_Option_ShouldPrefixKeys()
    {
        _i18Next.T("title", new { keyPrefix = "menu", name = "A" }).ShouldBe("Menu A");
        _i18Next.T("common:save", new { keyPrefix = "menu" }).ShouldBe("Save");
        _i18Next.TObject("items", new { keyPrefix = "menu" }).Count.ShouldBe(2);
        _i18Next.Exists("title", new { keyPrefix = "menu" }).ShouldBeTrue();
        (await _i18Next.ExistsAsync("de", "title", new { keyPrefix = "menu" })).ShouldBeTrue();
        _i18Next.T("title", new { keyPrefix = "" }).ShouldBe("title");
    }

    [Fact]
    public void ReturnEmptyString_Disabled_ShouldFallBack()
    {
        _i18Next.T("empty").ShouldBe("");

        _translator.ReturnEmptyString = false;
        _i18Next.SetFallbackLanguages("de");

        _i18Next.T("empty").ShouldBe("Leer");
        _i18Next.T("en", "empty", new { defaultValue = "Default" }).ShouldBe("Leer");
    }

    [Fact]
    public void FormatParams_ShouldBeMergedIntoIntlFormats()
    {
        _i18Next.T("price", new { value = 5, formatParams = new { value = new { minimumFractionDigits = 2 } } }).ShouldBe("5.00");
        _i18Next.T("amount", new { value = 5, formatParams = new { value = new { minimumFractionDigits = 0 } } }).ShouldBe("€5");
        _i18Next.T("shout", new { value = 1234, formatParams = new { value = new { useGrouping = false } } }).ShouldBe("1234");
        _i18Next.T("custom", new { value = 5, formatParams = new { value = new { minimumFractionDigits = 3 } } }).ShouldBe("5.0");
        _i18Next.T("price", new { value = 5, formatParams = new { other = new { minimumFractionDigits = 2 } } }).ShouldBe("5");
        _i18Next.T("price", new { value = 5, formatParams = new { value = new { } } }).ShouldBe("5");
        _i18Next.T("price", new Dictionary<string, object> { ["value"] = 5, ["formatParams"] = null }).ShouldBe("5");
    }
}
