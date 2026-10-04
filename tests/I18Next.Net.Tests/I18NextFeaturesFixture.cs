using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests;

[TestFixture]
public class I18NextFeaturesFixture
{
    private InMemoryBackend _backend;
    private DefaultTranslator _translator;
    private I18NextNet _i18Next;
    private List<string> _missingKeys;

    [SetUp]
    public void SetUp()
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
        _missingKeys = new List<string>();
        _translator.MissingKey += (_, args) => _missingKeys.Add(args.Key);

        _i18Next = new I18NextNet(_backend, _translator) { Language = "en" };
    }

    [TestCase(1, ExpectedResult = "1st place")]
    [TestCase(2, ExpectedResult = "2nd place")]
    [TestCase(3, ExpectedResult = "3rd place")]
    [TestCase(4, ExpectedResult = "4th place")]
    [TestCase(11, ExpectedResult = "11th place")]
    [TestCase(21, ExpectedResult = "21st place")]
    [TestCase(112, ExpectedResult = "112th place")]
    public string T_Ordinal_ShouldUseOrdinalSuffix(int count)
    {
        return _i18Next.T("place", new { count, ordinal = true });
    }

    [Test]
    public void T_OrdinalWithoutOrdinalKeys_ShouldFallBackToCategoryKey()
    {
        _i18Next.T("item", new { count = 1, ordinal = true }).Should().Be("1 item");
        _i18Next.T("item", new { count = 4, ordinal = true }).Should().Be("4 items");
    }

    [Test]
    public void T_DefaultValue_ShouldBeUsedForMissingKeys()
    {
        _i18Next.T("missing", new { defaultValue = "Default for {{name}}", name = "you" }).Should().Be("Default for you");
        _missingKeys.Should().Equal("missing");
    }

    [Test]
    public void T_DefaultValue_ShouldNotBeUsedForExistingKeys()
    {
        _i18Next.T("exampleKey", new { defaultValue = "Default" }).Should().Be("My English text.");
    }

    [Test]
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
        _i18Next.T("missing", args).Should().Be("1 default item");

        args["count"] = 5;
        _i18Next.T("missing", args).Should().Be("5 default items");

        args["count"] = 0;
        _i18Next.T("missing", args).Should().Be("no default items");

        args.Remove("defaultValue_other");
        args["count"] = 7;
        _i18Next.T("missing", args).Should().Be("fallback");
    }

    [Test]
    public void T_OrdinalDefaultValues_ShouldUseOrdinalForm()
    {
        _i18Next.T("missing", new Dictionary<string, object> { ["count"] = 2, ["ordinal"] = true, ["defaultValue_ordinal_two"] = "{{count}}nd" })
            .Should().Be("2nd");
        _i18Next.T("missing", new Dictionary<string, object> { ["count"] = 2, ["ordinal"] = true, ["defaultValue_two"] = "second" })
            .Should().Be("second");
    }

    [Test]
    public void T_MultipleKeys_ShouldUseFirstExistingKey()
    {
        _i18Next.T(new[] { "missing", "exampleKey" }).Should().Be("My English text.");
        _i18Next.T(new[] { "greeting", "exampleKey" }, new { name = "World" }).Should().Be("Hello World");
        _i18Next.T(new[] { "missing", "alsoMissing" }).Should().Be("alsoMissing");
        _i18Next.T(new[] { "missing", "alsoMissing" }, new { defaultValue = "Default" }).Should().Be("Default");
        _missingKeys.Should().Equal("alsoMissing", "alsoMissing");
    }

    [Test]
    public async Task Ta_MultipleKeys_ShouldUseFirstExistingKey()
    {
        (await _i18Next.Ta(new[] { "missing", "other:otherKey" })).Should().Be("Other namespace text.");
    }

    [Test]
    public void T_NoKeys_ShouldThrow()
    {
        _i18Next.Invoking(i => i.T(new string[0])).Should().Throw<System.ArgumentNullException>();
    }

    [Test]
    public async Task Exists_ShouldCheckTranslationsWithoutRaisingMissingKey()
    {
        _i18Next.Exists("exampleKey").Should().BeTrue();
        _i18Next.Exists("other:otherKey").Should().BeTrue();
        _i18Next.Exists("item", new { count = 3 }).Should().BeTrue();
        _i18Next.Exists("missing").Should().BeFalse();
        (await _i18Next.ExistsAsync("de", "exampleKey")).Should().BeTrue();
        (await _i18Next.ExistsAsync("de", "greeting")).Should().BeFalse();

        _missingKeys.Should().BeEmpty();
    }

    [Test]
    public void Exists_FallbackLanguage_ShouldBeConsidered()
    {
        _i18Next.SetFallbackLanguages("fr");

        _i18Next.Exists("frenchOnly").Should().BeTrue();
    }

    [TestCase("ar", ExpectedResult = "rtl")]
    [TestCase("ar-EG", ExpectedResult = "rtl")]
    [TestCase("he", ExpectedResult = "rtl")]
    [TestCase("fa_IR", ExpectedResult = "rtl")]
    [TestCase("pa-Arab-PK", ExpectedResult = "rtl")]
    [TestCase("en", ExpectedResult = "ltr")]
    [TestCase("de-DE", ExpectedResult = "ltr")]
    [TestCase("", ExpectedResult = "ltr")]
    public string Dir_ShouldReturnTextDirection(string language)
    {
        return _i18Next.Dir(language);
    }

    [Test]
    public void Dir_WithoutLanguage_ShouldUseCurrentLanguage()
    {
        _i18Next.Dir().Should().Be("ltr");

        _i18Next.Language = "ar";

        _i18Next.Dir().Should().Be("rtl");
    }

    [Test]
    public void LanguageFallbacks_ShouldTakePrecedenceOverGlobalFallbacks()
    {
        _i18Next.SetFallbackLanguages("fr");
        _i18Next.SetLanguageFallbacks("de-CH", "it");

        _i18Next.T("de-CH", "italianOnly").Should().Be("Testo italiano.");
        _i18Next.T("de-CH", "frenchOnly").Should().Be("frenchOnly");
        _i18Next.T("de-AT", "frenchOnly").Should().Be("Texte français.");
    }

    [Test]
    public void LanguageFallbacks_ShouldApplyToRegionsOfLanguage()
    {
        _i18Next.SetLanguageFallbacks("de", "it");

        _i18Next.T("de-CH", "italianOnly").Should().Be("Testo italiano.");
        _i18Next.LanguageFallbacks.Should().ContainKey("DE");
    }

    [Test]
    public void SetLanguageFallbacks_InvalidArguments_ShouldThrow()
    {
        _i18Next.Invoking(i => i.SetLanguageFallbacks(" ", "en")).Should().Throw<System.ArgumentNullException>();
        _i18Next.Invoking(i => i.SetLanguageFallbacks("de", null)).Should().Throw<System.ArgumentNullException>();
    }

    [Test]
    public void LanguageFallbacks_ShouldBeUsedWithNamespaceOverride()
    {
        _backend.AddTranslation("it", "other", "italianOther", "Altro testo.");
        _i18Next.SetLanguageFallbacks("de", "it");

        _i18Next.T("de", "other", "italianOther").Should().Be("Altro testo.");
    }

    [Test]
    public void NamespaceSeparator_Custom_ShouldSplitNamespace()
    {
        _translator.NamespaceSeparator = "::";

        _i18Next.T("other::otherKey").Should().Be("Other namespace text.");
        _i18Next.T("cimode", "other::otherKey").Should().Be("other::otherKey");
    }

    [Test]
    public void NamespaceSeparator_Disabled_ShouldUseKeysVerbatim()
    {
        _backend.AddTranslation("en", "translation", "Note: read this", "Hinweis");
        _translator.NamespaceSeparator = null;

        _i18Next.T("Note: read this").Should().Be("Hinweis");
    }

    [Test]
    public void ClearCache_ShouldReloadNamespaces()
    {
        _i18Next.T("es", "exampleKey").Should().Be("exampleKey");

        _backend.AddTranslation("es", "translation", "exampleKey", "Texto en español.");

        _i18Next.T("es", "exampleKey").Should().Be("exampleKey");

        _translator.ClearCache("es", "translation");

        _i18Next.T("es", "exampleKey").Should().Be("Texto en español.");

        _backend.RemoveNamespace("es", "translation").Should().BeTrue();
        _translator.ClearCache();

        _i18Next.T("es", "exampleKey").Should().Be("exampleKey");
    }

    [Test]
    public void InMemoryBackend_AddTranslations_ShouldAddAllTranslations()
    {
        _backend.AddTranslations("nl", "translation", new Dictionary<string, string> { ["a"] = "A", ["b"] = "B" });

        _backend.HasNamespace("nl", "translation").Should().BeTrue();
        _backend.HasNamespace("nl", "missing").Should().BeFalse();
        _i18Next.T("nl", "b").Should().Be("B");
        _backend.Invoking(b => b.AddTranslations("nl", "translation", null)).Should().Throw<System.ArgumentNullException>();
    }
}
