using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class DefaultTranslatorVersion4Fixture
{
    private DefaultTranslator _translator;
    private TranslationOptions _options;

    [SetUp]
    public void SetUp()
    {
        var backend = new InMemoryBackend();

        backend.AddTranslation("en", "translation", "item_one", "{{count}} item");
        backend.AddTranslation("en", "translation", "item_other", "{{count}} items");
        backend.AddTranslation("en", "translation", "item_zero", "No items");
        backend.AddTranslation("en", "translation", "friend_one", "A friend");
        backend.AddTranslation("en", "translation", "friend_other", "{{count}} friends");
        backend.AddTranslation("en", "translation", "friend_male_one", "A boyfriend");
        backend.AddTranslation("en", "translation", "friend_male_other", "{{count}} boyfriends");
        backend.AddTranslation("en", "translation", "friend_male_zero", "No boyfriends");
        backend.AddTranslation("ar", "translation", "item_zero", "zero");
        backend.AddTranslation("ar", "translation", "item_few", "few");
        backend.AddTranslation("ru", "translation", "item_one", "one");
        backend.AddTranslation("ru", "translation", "item_few", "few");
        backend.AddTranslation("ru", "translation", "item_many", "many");

        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var logger = new TraceLogger();

        _translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));
        _options = new TranslationOptions { DefaultNamespace = "translation" };
    }

    private Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args)
    {
        return _translator.TranslateAsync(language, key, args, _options);
    }

    [TestCase(1, ExpectedResult = "1 item")]
    [TestCase(2, ExpectedResult = "2 items")]
    [TestCase(0, ExpectedResult = "No items")]
    public async Task<string> TranslateAsync_English_ShouldUseCategorySuffix(int count)
    {
        return await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = count });
    }

    [Test]
    public async Task TranslateAsync_ZeroWithoutZeroKey_ShouldUseCategorySuffix()
    {
        var result = await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = 0 });

        result.Should().Be("0 friends");
    }

    [Test]
    public async Task TranslateAsync_LongCount_ShouldUseCategorySuffix()
    {
        var result = await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = 1L });

        result.Should().Be("1 item");
    }

    [TestCase(0, ExpectedResult = "No boyfriends")]
    [TestCase(1, ExpectedResult = "A boyfriend")]
    [TestCase(5, ExpectedResult = "5 boyfriends")]
    public async Task<string> TranslateAsync_ContextAndPlural_ShouldPreferContextKeys(int count)
    {
        return await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = count, ["context"] = "male" });
    }

    [Test]
    public async Task TranslateAsync_UnknownContext_ShouldFallBackToPluralKey()
    {
        var result = await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = 3, ["context"] = "female" });

        result.Should().Be("3 friends");
    }

    [TestCase("ar", 0, ExpectedResult = "zero")]
    [TestCase("ar", 5, ExpectedResult = "few")]
    [TestCase("ru", 1, ExpectedResult = "one")]
    [TestCase("ru", 3, ExpectedResult = "few")]
    [TestCase("ru", 7, ExpectedResult = "many")]
    public async Task<string> TranslateAsync_OtherLanguages_ShouldUseCategorySuffix(string language, int count)
    {
        return await TranslateAsync(language, "item", new Dictionary<string, object> { ["count"] = count });
    }

    [Test]
    public async Task TranslateAsync_MissingKey_ShouldReportAllPossibleKeys()
    {
        string[] possibleKeys = null;
        _translator.MissingKey += (_, args) => possibleKeys = args.PossibleKeys;

        var result = await TranslateAsync("en", "missing", new Dictionary<string, object> { ["count"] = 0, ["context"] = "ctx" });

        result.Should().Be("missing");
        possibleKeys.Should().Equal("missing", "missing_other", "missing_zero", "missing_ctx", "missing_ctx_other", "missing_ctx_zero");
    }

    [Test]
    public async Task TranslateAsync_NullCount_ShouldIgnorePlural()
    {
        var result = await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = null });

        result.Should().Be("item");
    }

    [Test]
    public async Task TranslateAsync_CiMode_ShouldReturnNamespaceAndKey()
    {
        var result = await TranslateAsync("CIMODE", "item", null);

        result.Should().Be("translation:item");
    }
}
