using System.Collections.Generic;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class PostProcessorsFixture
{
    [Fact]
    public void SprintfPostProcessor_WithArguments_ShouldReplacePlaceholdersInOrder()
    {
        var postProcessor = new SprintfPostProcessor();
        var args = new Dictionary<string, object> { ["sprintf"] = new object[] { "a", "b", 3 } };

        postProcessor.ProcessResult("key", "The first 3 letters: %s, %s and {%d}", args, "en", null)
            .ShouldBe("The first 3 letters: a, b and {3}");
    }

    [Fact]
    public void SprintfPostProcessor_WithoutArguments_ShouldReturnValue()
    {
        var postProcessor = new SprintfPostProcessor();

        postProcessor.Keyword.ShouldBe("sprintf");
        postProcessor.ProcessTranslation("key", "%s", null, "en", null).ShouldBe("%s");
        postProcessor.ProcessResult("key", "%s", null, "en", null).ShouldBe("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object>(), "en", null).ShouldBe("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object> { ["sprintf"] = null }, "en", null).ShouldBe("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object> { ["sprintf"] = "a" }, "en", null).ShouldBe("%s");
    }

    [Theory]
    [InlineData(0, "none")]
    [InlineData(-5, "none")]
    [InlineData(1, "one")]
    [InlineData(3, "a few")]
    [InlineData(7, "many")]
    public void IntervalPostProcessor_ShouldPickMatchingInterval(int count, string expected)
    {
        var postProcessor = new IntervalPostProcessor();

        postProcessor.ProcessResult("key", "(inf-0){none};(1){one};(2-5){a few};(6-inf){many};", new Dictionary<string, object> { ["count"] = count }, "en", null).ShouldBe(expected);
    }

    [Fact]
    public void IntervalPostProcessor_NoMatch_ShouldReturnValueOrFirstInterval()
    {
        var postProcessor = new IntervalPostProcessor();
        const string value = "(1){one};(2-x){invalid};(y-inf){invalid};(inf-z){invalid};(z){invalid}";

        postProcessor.Keyword.ShouldBe("interval");
        postProcessor.ProcessTranslation("key", value, null, "en", null).ShouldBe(value);
        postProcessor.ProcessResult("key", value, new Dictionary<string, object> { ["count"] = 5 }, "en", null).ShouldBe(value);

        postProcessor.UseFirstAsFallback = true;

        postProcessor.ProcessResult("key", value, null, "en", null).ShouldBe("one");
        postProcessor.ProcessResult("key", "plain", null, "en", null).ShouldBe("plain");
    }

    [Fact]
    public void PseudoLocalizationPostProcessor_ConfiguredLanguage_ShouldReplaceAndRepeatLetters()
    {
        var options = new PseudoLocalizationOptions { LetterMultiplier = 3 };
        options.LanguagesToPseudo.Add("en");

        var postProcessor = new PseudoLocalizationPostProcessor(options);

        postProcessor.Keyword.ShouldBe("pseudo");
        postProcessor.Options.ShouldBeSameAs(options);
        postProcessor.ProcessTranslation("key", "abc", null, "en", null).ShouldBe("abc");
        postProcessor.ProcessResult("key", "ab 1", null, "en", null).ShouldBe("αααḅ 1");
        postProcessor.ProcessResult("key", "ab 1", null, "de", null).ShouldBe("ab 1");
    }

    [Fact]
    public void PseudoLocalizationOptions_InvalidValues_ShouldThrow()
    {
        var options = new PseudoLocalizationOptions();

        Should.Throw<System.ArgumentOutOfRangeException>(() => options.LetterMultiplier = 0);
        Should.Throw<System.ArgumentOutOfRangeException>(() => options.LetterMultiplier = 101);
        Should.Throw<System.ArgumentNullException>(() => options.RepeatedLetters = null);
        Should.Throw<System.ArgumentNullException>(() => options.Letters = null);

        options.RepeatedLetters = new[] { 'x' };
        options.Letters = new Dictionary<char, char> { ['x'] = 'y' };
        options.WrapStrings = true;

        options.RepeatedLetters.ShouldBe(new[] { 'x' });
        options.Letters.ShouldContainKey('x');
        options.WrapStrings.ShouldBeTrue();
    }

    [Fact]
    public void Translator_PostProcessArgument_ShouldApplyMatchingPostProcessors()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "sprintf", "Hello %s");
        backend.AddTranslation("en", "translation", "interval", "(1){one item};(2-inf){many items};");

        var translator = new DefaultTranslator(backend);
        translator.PostProcessors.Add(new SprintfPostProcessor());
        translator.PostProcessors.Add(new IntervalPostProcessor());

        var i18Next = new I18NextNet(backend, translator) { Language = "en" };

        i18Next.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "World" } }).ShouldBe("Hello World");
        i18Next.T("interval", new { postProcess = "interval", count = 1 }).ShouldBe("one item");
        i18Next.T("interval", new { postProcess = new[] { "interval" }, count = 3 }).ShouldBe("many items");
        i18Next.T("sprintf", new { postProcess = "interval, sprintf", sprintf = new[] { "You" } }).ShouldBe("Hello You");
        i18Next.T("sprintf", new { postProcess = 5 }).ShouldBe("Hello %s");

        translator.AllowPostprocessing = false;

        i18Next.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "World" } }).ShouldBe("Hello %s");
    }
}
