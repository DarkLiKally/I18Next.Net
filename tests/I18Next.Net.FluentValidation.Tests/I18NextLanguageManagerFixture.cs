using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using FluentValidation.Resources;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.FluentValidation.Tests;

public class I18NextLanguageManagerFixture
{
    public I18NextLanguageManagerFixture()
    {
        _backend = new InMemoryBackend();

        _backend.AddTranslation("en", "validation", "NotEmptyValidator", "{{PropertyName}} is required!");
        _backend.AddTranslation("en", "validation", "NotNullValidator", "$t(NotEmptyValidator)");
        _backend.AddTranslation("en", "validation", "LengthValidator", "{{- PropertyName}} needs {{ MinLength }} to {MaxLength:N0} characters.");
        _backend.AddTranslation("en", "validation", "fluent.NotEmptyValidator", "{{PropertyName}} is empty.");
        _backend.AddTranslation("en", "validation", "CustomCode", "Custom {{PropertyName}}.");
        _backend.AddTranslation("en", "messages", "NotEmptyValidator", "{{PropertyName}} from messages.");
        _backend.AddTranslation("de", "validation", "NotEmptyValidator", "{{PropertyName}} wird benötigt!");

        _i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend)) { Language = "en" };
        _languageManager = new I18NextLanguageManager(_i18Next);
    }

    private readonly InMemoryBackend _backend;
    private readonly I18NextNet _i18Next;
    private readonly I18NextLanguageManager _languageManager;

    public static TheoryData<string, string> DefaultMessages()
    {
        var data = new TheoryData<string, string>();

        foreach (var language in new[] { "en", "de" })
            foreach (var key in GetKeys(language))
                data.Add(language, key);

        return data;
    }

    [Theory]
    [MemberData(nameof(DefaultMessages))]
    public void GetString_DefaultMessages_ShouldMatchFluentValidationMessages(string language, string key)
    {
        var backend = new JsonFileBackend(Path.Combine(AppContext.BaseDirectory, "Resources"));
        var languageManager = new I18NextLanguageManager(new I18NextNet(backend, new DefaultTranslator(backend)) { Language = language });

        languageManager.GetString(key).ShouldBe(new LanguageManager().GetString(key, new CultureInfo(language)));
    }

    [Fact]
    public void DefaultMessages_AllLanguages_ShouldContainSameKeys()
    {
        GetKeys("de").ShouldBe(GetKeys("en"));
    }

    [Fact]
    public void GetString_Translation_ShouldConvertPlaceholders()
    {
        _languageManager.GetString("NotEmptyValidator").ShouldBe("{PropertyName} is required!");
        _languageManager.GetString("LengthValidator").ShouldBe("{PropertyName} needs {MinLength} to {MaxLength:N0} characters.");
    }

    [Fact]
    public void GetString_NestedTranslation_ShouldResolveNesting()
    {
        _languageManager.GetString("NotNullValidator").ShouldBe("{PropertyName} is required!");
    }

    [Fact]
    public void GetString_ErrorCode_ShouldTranslate()
    {
        _languageManager.GetString("CustomCode").ShouldBe("Custom {PropertyName}.");
    }

    [Fact]
    public void GetString_MissingKey_ShouldUseBuiltInMessage()
    {
        _languageManager.GetString("EmailValidator").ShouldBe("'{PropertyName}' is not a valid email address.");
        _languageManager.GetString("EmailValidator", new CultureInfo("fr")).ShouldBe(new LanguageManager().GetString("EmailValidator", new CultureInfo("fr")));
    }

    [Fact]
    public void GetString_MissingKeyInLanguage_ShouldUseBuiltInMessageOfLanguage()
    {
        _i18Next.Language = "fr";

        _languageManager.GetString("NotEmptyValidator").ShouldBe(new LanguageManager().GetString("NotEmptyValidator", new CultureInfo("fr")));
    }

    [Fact]
    public void GetString_UnknownLanguage_ShouldUseBuiltInMessageOfCurrentCulture()
    {
        _i18Next.Language = "xx-invalid-language-tag-!";

        _languageManager.GetString("EmailValidator").ShouldBe(new LanguageManager().GetString("EmailValidator", CultureInfo.CurrentUICulture));
    }

    [Fact]
    public void GetString_UnknownKey_ShouldReturnEmptyString()
    {
        _languageManager.GetString("UnknownValidator").ShouldBeEmpty();
        _languageManager.GetString(null).ShouldBeEmpty();
    }

    [Fact]
    public void GetString_Culture_ShouldUseCulture()
    {
        _languageManager.GetString("NotEmptyValidator", new CultureInfo("de")).ShouldBe("{PropertyName} wird benötigt!");
    }

    [Fact]
    public void GetString_CultureProperty_ShouldUseCulture()
    {
        _languageManager.Culture = new CultureInfo("de");

        _languageManager.GetString("NotEmptyValidator").ShouldBe("{PropertyName} wird benötigt!");
        _languageManager.GetString("NotEmptyValidator", new CultureInfo("en")).ShouldBe("{PropertyName} is required!");
    }

    [Fact]
    public void GetString_InvariantCulture_ShouldUseCurrentLanguage()
    {
        _i18Next.Language = "de";

        _languageManager.GetString("NotEmptyValidator", CultureInfo.InvariantCulture).ShouldBe("{PropertyName} wird benötigt!");
    }

    [Fact]
    public void GetString_DetectLanguageOnEachTranslation_ShouldUseDetectedLanguage()
    {
        var i18Next = new I18NextNet(_backend, new DefaultTranslator(_backend), new DefaultLanguageDetector("de"))
        {
            Language = "en",
            DetectLanguageOnEachTranslation = true
        };

        new I18NextLanguageManager(i18Next).GetString("NotEmptyValidator").ShouldBe("{PropertyName} wird benötigt!");
    }

    [Fact]
    public void GetString_KeyPrefix_ShouldUsePrefix()
    {
        var languageManager = new I18NextLanguageManager(_i18Next, new I18NextFluentValidationOptions { KeyPrefix = "fluent" });

        languageManager.GetString("NotEmptyValidator").ShouldBe("{PropertyName} is empty.");
        languageManager.GetString("EmailValidator").ShouldBe("'{PropertyName}' is not a valid email address.");
    }

    [Fact]
    public void GetString_Namespace_ShouldUseNamespace()
    {
        var languageManager = new I18NextLanguageManager(_i18Next, new I18NextFluentValidationOptions { Namespace = "messages" });

        languageManager.GetString("NotEmptyValidator").ShouldBe("{PropertyName} from messages.");
    }

    [Fact]
    public void GetString_Disabled_ShouldUseEnglishBuiltInMessage()
    {
        _languageManager.Enabled = false;

        _languageManager.Enabled.ShouldBeFalse();
        _languageManager.GetString("NotEmptyValidator", new CultureInfo("de")).ShouldBe("'{PropertyName}' must not be empty.");
    }

    [Fact]
    public void GetString_InvalidKey_ShouldUseBuiltInMessage()
    {
        var i18Next = Substitute.For<II18Next>();
        i18Next.Language.Returns("en");
        i18Next.ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<bool>(new TranslationKeyInvalidException("NotEmptyValidator")));

        new I18NextLanguageManager(i18Next).GetString("NotEmptyValidator").ShouldBe("'{PropertyName}' must not be empty.");
    }

    [Fact]
    public void Constructor_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new I18NextLanguageManager(null));
        Should.Throw<ArgumentNullException>(() => new I18NextLanguageManager(_i18Next, null));
    }

    [Fact]
    public void Options_EmptyNamespace_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => new I18NextFluentValidationOptions { Namespace = string.Empty });
    }

    private static IEnumerable<string> GetKeys(string language)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Resources", language, "validation.json")));

        return document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
    }
}
