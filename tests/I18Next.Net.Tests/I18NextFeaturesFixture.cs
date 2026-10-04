using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests;

public class I18NextFeaturesFixture
{
    public I18NextFeaturesFixture()
    {
        _backend = new InMemoryBackend();

        _backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
        _backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}");
        _backend.AddTranslation("en", "translation", "place_ordinal_one", "{{count}}st place");
        _backend.AddTranslation("en", "translation", "place_ordinal_two", "{{count}}nd place");
        _backend.AddTranslation("en", "translation", "place_ordinal_few", "{{count}}rd place");
        _backend.AddTranslation("en", "translation", "place_ordinal_other", "{{count}}th place");
        _backend.AddTranslation("en", "translation", "item_one", "{{count}} item");
        _backend.AddTranslation("en", "translation", "item_other", "{{count}} items");
        _backend.AddTranslation("en", "other", "otherKey", "Other namespace text.");
        _backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");
        _backend.AddTranslation("fr", "translation", "frenchOnly", "Texte français.");
        _backend.AddTranslation("it", "translation", "italianOnly", "Testo italiano.");

        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var logger = new TraceLogger();

        _translator = new DefaultTranslator(_backend, logger, pluralResolver, new DefaultInterpolator(logger));
        _missingKeys = [];
        _translator.MissingKey += (_, args) => _missingKeys.Add(args.Key);

        _i18Next = new I18NextNet(_backend, _translator) { Language = "en" };
    }
    private readonly InMemoryBackend _backend;
    private readonly DefaultTranslator _translator;
    private readonly I18NextNet _i18Next;
    private readonly List<string> _missingKeys;


    [Theory]
    [InlineData(1, "1st place")]
    [InlineData(2, "2nd place")]
    [InlineData(3, "3rd place")]
    [InlineData(4, "4th place")]
    [InlineData(11, "11th place")]
    [InlineData(21, "21st place")]
    [InlineData(112, "112th place")]
    public void T_Ordinal_ShouldUseOrdinalSuffix(int count, string expected)
    {
        _i18Next.T("place", new { count, ordinal = true }).ShouldBe(expected);
    }

    [Fact]
    public void T_OrdinalWithoutOrdinalKeys_ShouldFallBackToCategoryKey()
    {
        _i18Next.T("item", new { count = 1, ordinal = true }).ShouldBe("1 item");
        _i18Next.T("item", new { count = 4, ordinal = true }).ShouldBe("4 items");
    }

    [Fact]
    public void T_DefaultValue_ShouldBeUsedForMissingKeys()
    {
        _i18Next.T("missing", new { defaultValue = "Default for {{name}}", name = "you" }).ShouldBe("Default for you");
        _missingKeys.ShouldBe(new[] { "missing" });
    }

    [Fact]
    public void T_DefaultValue_ShouldNotBeUsedForExistingKeys()
    {
        _i18Next.T("exampleKey", new { defaultValue = "Default" }).ShouldBe("My English text.");
    }

    [Fact]
    public void T_PluralDefaultValues_ShouldUseMatchingPluralForm()
    {
        var args = new Dictionary<string, object>
        {
            ["defaultValue"] = "fallback",
            ["defaultValue_one"] = "{{count}} default item",
            ["defaultValue_other"] = "{{count}} default items",
            ["defaultValue_zero"] = "no default items"
        };

        args["count"] = 1;
        _i18Next.T("missing", args).ShouldBe("1 default item");

        args["count"] = 5;
        _i18Next.T("missing", args).ShouldBe("5 default items");

        args["count"] = 0;
        _i18Next.T("missing", args).ShouldBe("no default items");

        args.Remove("defaultValue_other");
        args["count"] = 7;
        _i18Next.T("missing", args).ShouldBe("fallback");
    }

    [Fact]
    public void T_OrdinalDefaultValues_ShouldUseOrdinalForm()
    {
        _i18Next.T("missing", new Dictionary<string, object> { ["count"] = 2, ["ordinal"] = true, ["defaultValue_ordinal_two"] = "{{count}}nd" })
            .ShouldBe("2nd");
        _i18Next.T("missing", new Dictionary<string, object> { ["count"] = 2, ["ordinal"] = true, ["defaultValue_two"] = "second" })
            .ShouldBe("second");
    }

    [Fact]
    public void T_MultipleKeys_ShouldUseFirstExistingKey()
    {
        _i18Next.T(["missing", "exampleKey"]).ShouldBe("My English text.");
        _i18Next.T(["greeting", "exampleKey"], new { name = "World" }).ShouldBe("Hello World");
        _i18Next.T(["missing", "alsoMissing"]).ShouldBe("alsoMissing");
        _i18Next.T(["missing", "alsoMissing"], new { defaultValue = "Default" }).ShouldBe("Default");
        _missingKeys.ShouldBe(new[] { "alsoMissing", "alsoMissing" });
    }

    [Fact]
    public async Task Ta_MultipleKeys_ShouldUseFirstExistingKey()
    {
        (await _i18Next.Ta(["missing", "other:otherKey"])).ShouldBe("Other namespace text.");
    }

    [Fact]
    public void T_NoKeys_ShouldThrow()
    {
        Should.Throw<System.ArgumentNullException>(() => _i18Next.T([]));
    }

    [Fact]
    public async Task Exists_ShouldCheckTranslationsWithoutRaisingMissingKey()
    {
        _i18Next.Exists("exampleKey").ShouldBeTrue();
        _i18Next.Exists("other:otherKey").ShouldBeTrue();
        _i18Next.Exists("item", new { count = 3 }).ShouldBeTrue();
        _i18Next.Exists("missing").ShouldBeFalse();
        (await _i18Next.ExistsAsync("de", "exampleKey")).ShouldBeTrue();
        (await _i18Next.ExistsAsync("de", "greeting")).ShouldBeFalse();

        _missingKeys.ShouldBeEmpty();
    }

    [Fact]
    public void Exists_FallbackLanguage_ShouldBeConsidered()
    {
        _i18Next.SetFallbackLanguages("fr");

        _i18Next.Exists("frenchOnly").ShouldBeTrue();
    }

    [Theory]
    [InlineData("ar", "rtl")]
    [InlineData("ar-EG", "rtl")]
    [InlineData("he", "rtl")]
    [InlineData("fa_IR", "rtl")]
    [InlineData("pa-Arab-PK", "rtl")]
    [InlineData("en", "ltr")]
    [InlineData("de-DE", "ltr")]
    [InlineData("", "ltr")]
    public void Dir_ShouldReturnTextDirection(string language, string expected)
    {
        _i18Next.Dir(language).ShouldBe(expected);
    }

    [Fact]
    public void Dir_WithoutLanguage_ShouldUseCurrentLanguage()
    {
        _i18Next.Dir().ShouldBe("ltr");

        _i18Next.Language = "ar";

        _i18Next.Dir().ShouldBe("rtl");
    }

    [Fact]
    public void LanguageFallbacks_ShouldTakePrecedenceOverGlobalFallbacks()
    {
        _i18Next.SetFallbackLanguages("fr");
        _i18Next.SetLanguageFallbacks("de-CH", "it");

        _i18Next.T("de-CH", "italianOnly").ShouldBe("Testo italiano.");
        _i18Next.T("de-CH", "frenchOnly").ShouldBe("frenchOnly");
        _i18Next.T("de-AT", "frenchOnly").ShouldBe("Texte français.");
    }

    [Fact]
    public void LanguageFallbacks_ShouldApplyToRegionsOfLanguage()
    {
        _i18Next.SetLanguageFallbacks("de", "it");

        _i18Next.T("de-CH", "italianOnly").ShouldBe("Testo italiano.");
        _i18Next.LanguageFallbacks.ShouldContainKey("DE");
    }

    [Fact]
    public void SetLanguageFallbacks_InvalidArguments_ShouldThrow()
    {
        Should.Throw<System.ArgumentNullException>(() => _i18Next.SetLanguageFallbacks(" ", "en"));
        Should.Throw<System.ArgumentNullException>(() => _i18Next.SetLanguageFallbacks("de", null));
    }

    [Fact]
    public void LanguageFallbacks_ShouldBeUsedWithNamespaceOverride()
    {
        _backend.AddTranslation("it", "other", "italianOther", "Altro testo.");
        _i18Next.SetLanguageFallbacks("de", "it");

        _i18Next.T("de", "other", "italianOther").ShouldBe("Altro testo.");
    }

    [Fact]
    public void NamespaceSeparator_Custom_ShouldSplitNamespace()
    {
        _translator.NamespaceSeparator = "::";

        _i18Next.T("other::otherKey").ShouldBe("Other namespace text.");
        _i18Next.T("cimode", "other::otherKey").ShouldBe("other::otherKey");
    }

    [Fact]
    public void NamespaceSeparator_Disabled_ShouldUseKeysVerbatim()
    {
        _backend.AddTranslation("en", "translation", "Note: read this", "Hinweis");
        _translator.NamespaceSeparator = null;

        _i18Next.T("Note: read this").ShouldBe("Hinweis");
    }

    [Fact]
    public void ClearCache_ShouldReloadNamespaces()
    {
        _i18Next.T("es", "exampleKey").ShouldBe("exampleKey");

        _backend.AddTranslation("es", "translation", "exampleKey", "Texto en español.");

        _i18Next.T("es", "exampleKey").ShouldBe("exampleKey");

        _translator.ClearCache("es", "translation");

        _i18Next.T("es", "exampleKey").ShouldBe("Texto en español.");

        _backend.RemoveNamespace("es", "translation").ShouldBeTrue();
        _translator.ClearCache();

        _i18Next.T("es", "exampleKey").ShouldBe("exampleKey");
    }

    [Fact]
    public void InMemoryBackend_AddTranslations_ShouldAddAllTranslations()
    {
        _backend.AddTranslations("nl", "translation", new Dictionary<string, string> { ["a"] = "A", ["b"] = "B" });

        _backend.HasNamespace("nl", "translation").ShouldBeTrue();
        _backend.HasNamespace("nl", "missing").ShouldBeFalse();
        _i18Next.T("nl", "b").ShouldBe("B");
        Should.Throw<System.ArgumentNullException>(() => _backend.AddTranslations("nl", "translation", null));
    }
}
