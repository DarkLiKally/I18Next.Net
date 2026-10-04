using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class DefaultTranslatorVersion4Fixture
{
    public DefaultTranslatorVersion4Fixture()
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
        backend.AddTranslation("ru", "translation", "item_other", "other");
        backend.AddTranslation("fr", "translation", "item_one", "{{count}} élément");
        backend.AddTranslation("fr", "translation", "item_other", "{{count}} éléments");
        backend.AddTranslation("en", "translation", "place_ordinal_one", "{{count}}st");
        backend.AddTranslation("en", "translation", "place_ordinal_other", "{{count}}th");

        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var logger = new TraceLogger();

        _translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));
        _options = new TranslationOptions { DefaultNamespace = "translation" };
    }
    private readonly DefaultTranslator _translator;
    private readonly TranslationOptions _options;


    private Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args)
    {
        return _translator.TranslateAsync(language, key, args, _options);
    }

    [Theory]
    [InlineData(1, "1 item")]
    [InlineData(2, "2 items")]
    [InlineData(0, "No items")]
    public async Task TranslateAsync_English_ShouldUseCategorySuffix(int count, string expected)
    {
        (await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = count })).ShouldBe(expected);
    }

    [Fact]
    public async Task TranslateAsync_ZeroWithoutZeroKey_ShouldUseCategorySuffix()
    {
        var result = await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = 0 });

        result.ShouldBe("0 friends");
    }

    [Fact]
    public async Task TranslateAsync_LongCount_ShouldUseCategorySuffix()
    {
        var result = await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = 1L });

        result.ShouldBe("1 item");
    }

    [Theory]
    [InlineData(0, "No boyfriends")]
    [InlineData(1, "A boyfriend")]
    [InlineData(5, "5 boyfriends")]
    public async Task TranslateAsync_ContextAndPlural_ShouldPreferContextKeys(int count, string expected)
    {
        (await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = count, ["context"] = "male" })).ShouldBe(expected);
    }

    [Fact]
    public async Task TranslateAsync_UnknownContext_ShouldFallBackToPluralKey()
    {
        var result = await TranslateAsync("en", "friend", new Dictionary<string, object> { ["count"] = 3, ["context"] = "female" });

        result.ShouldBe("3 friends");
    }

    [Theory]
    [InlineData("ar", 0, "zero")]
    [InlineData("ar", 5, "few")]
    [InlineData("ru", 1, "one")]
    [InlineData("ru", 3, "few")]
    [InlineData("ru", 7, "many")]
    public async Task TranslateAsync_OtherLanguages_ShouldUseCategorySuffix(string language, int count, string expected)
    {
        (await TranslateAsync(language, "item", new Dictionary<string, object> { ["count"] = count })).ShouldBe(expected);
    }

    [Fact]
    public async Task TranslateAsync_MissingKey_ShouldReportAllPossibleKeys()
    {
        string[] possibleKeys = null;
        _translator.MissingKey += (_, args) => possibleKeys = args.PossibleKeys;

        var result = await TranslateAsync("en", "missing", new Dictionary<string, object> { ["count"] = 0, ["context"] = "ctx" });

        result.ShouldBe("missing");
        possibleKeys.ShouldBe(["missing", "missing_other", "missing_zero", "missing_ctx", "missing_ctx_other", "missing_ctx_zero"]);
    }

    [Fact]
    public async Task TranslateAsync_NullCount_ShouldIgnorePlural()
    {
        var result = await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = null });

        result.ShouldBe("item");
    }

    [Fact]
    public async Task TranslateAsync_CiMode_ShouldReturnNamespaceAndKey()
    {
        var result = await TranslateAsync("CIMODE", "item", null);

        result.ShouldBe("translation:item");
    }

    [Theory]
    [InlineData("en", 1.5, "1.5 items")]
    [InlineData("en", 1.0, "1 item")]
    [InlineData("fr", 1.5, "1.5 élément")]
    [InlineData("fr", 2.5, "2.5 éléments")]
    [InlineData("ru", 1.5, "other")]
    [InlineData("ru", 21.0, "one")]
    public async Task TranslateAsync_DecimalCount_ShouldUseCldrCategories(string language, double count, string expected)
    {
        (await TranslateAsync(language, "item", new Dictionary<string, object> { ["count"] = count })).ShouldBe(expected);
    }

    [Fact]
    public async Task TranslateAsync_DecimalAndFloatCounts_ShouldBeSupported()
    {
        (await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = 0.5m })).ShouldBe("0.5 items");
        (await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = 1f })).ShouldBe("1 item");
        (await TranslateAsync("en", "item", new Dictionary<string, object> { ["count"] = (short)1 })).ShouldBe("1 item");
    }

    [Fact]
    public async Task TranslateAsync_DecimalOrdinal_ShouldUseOther()
    {
        (await TranslateAsync("en", "place", new Dictionary<string, object> { ["count"] = 1.5, ["ordinal"] = true })).ShouldBe("1.5th");
        (await TranslateAsync("en", "place", new Dictionary<string, object> { ["count"] = 1.0, ["ordinal"] = true })).ShouldBe("1st");
    }
}
