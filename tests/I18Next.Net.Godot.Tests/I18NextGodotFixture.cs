using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Godot.Tests;

public class I18NextGodotFixture
{
    private static I18NextNet CreateI18Next()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "title", "Hello");
        backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}");
        backend.AddTranslation("en", "translation", "inbox_one", "One message");
        backend.AddTranslation("en", "translation", "inbox_other", "{{count}} messages");
        backend.AddTranslation("en", "translation", "friend", "A friend");
        backend.AddTranslation("en", "translation", "friend_male", "A boyfriend");
        backend.AddTranslation("en", "common", "ok", "OK");
        backend.AddTranslation("de", "translation", "title", "Hallo");

        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));

        return new I18NextNet(backend, translator) { Language = "en" };
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("de-DE", "de_DE")]
    [InlineData("zh-Hans-CN", "zh_Hans_CN")]
    [InlineData(null, null)]
    public void ToLocale_Language_ShouldUseUnderscores(string language, string expected)
    {
        I18NextGodot.ToLocale(language).ShouldBe(expected);
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("de_DE", "de-DE")]
    [InlineData("zh_Hans_CN", "zh-Hans-CN")]
    [InlineData(null, null)]
    public void ToLanguage_Locale_ShouldUseHyphens(string locale, string expected)
    {
        I18NextGodot.ToLanguage(locale).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en", "Hello")]
    [InlineData("de", "Hallo")]
    [InlineData("de-DE", "Hallo")]
    public void Translate_ExistingKey_ShouldTranslateIntoLanguage(string language, string expected)
    {
        I18NextGodot.Translate(CreateI18Next(), language, "title", null, null).ShouldBe(expected);
    }

    [Fact]
    public void Translate_LanguageDiffersFromInstance_ShouldNotChangeLanguage()
    {
        var i18Next = CreateI18Next();

        I18NextGodot.Translate(i18Next, "de", "title", null, null);

        i18Next.Language.ShouldBe("en");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("Score: 10")]
    [InlineData("Press start.")]
    [InlineData("")]
    [InlineData(null)]
    public void Translate_MissingKey_ShouldReturnNull(string key)
    {
        I18NextGodot.Translate(CreateI18Next(), "en", key, null, null).ShouldBeNull();
    }

    [Fact]
    public void Translate_KeyMissingInLanguage_ShouldReturnNull()
    {
        I18NextGodot.Translate(CreateI18Next(), "de", "greeting", null, null).ShouldBeNull();
    }

    [Fact]
    public void Translate_Namespace_ShouldTranslateFromNamespace()
    {
        I18NextGodot.Translate(CreateI18Next(), "en", "common:ok", null, null).ShouldBe("OK");
    }

    [Theory]
    [InlineData(1, "One message")]
    [InlineData(5, "5 messages")]
    [InlineData(0, "0 messages")]
    public void Translate_Count_ShouldResolvePlural(int count, string expected)
    {
        I18NextGodot.Translate(CreateI18Next(), "en", "inbox", null, count).ShouldBe(expected);
    }

    [Theory]
    [InlineData("male", "A boyfriend")]
    [InlineData("female", "A friend")]
    [InlineData("", "A friend")]
    [InlineData(null, "A friend")]
    public void Translate_Context_ShouldResolveContext(string context, string expected)
    {
        I18NextGodot.Translate(CreateI18Next(), "en", "friend", context, null).ShouldBe(expected);
    }

    [Fact]
    public void Translate_WithoutInstance_ShouldReturnNull()
    {
        I18NextGodot.Translate(null, "en", "title", null, null).ShouldBeNull();
    }

    [Fact]
    public void Translate_WithoutLanguage_ShouldReturnNull()
    {
        I18NextGodot.Translate(CreateI18Next(), null, "title", null, null).ShouldBeNull();
    }

    [Fact]
    public void T_WithoutRegisteredInstance_ShouldReturnKey()
    {
        GodotObjectExtensions.T(null, "title").ShouldBe("title");
    }
}
