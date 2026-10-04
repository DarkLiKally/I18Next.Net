using System.Collections.Generic;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class PostProcessorsFixture
{
    [Test]
    public void SprintfPostProcessor_WithArguments_ShouldReplacePlaceholdersInOrder()
    {
        var postProcessor = new SprintfPostProcessor();
        var args = new Dictionary<string, object> { ["sprintf"] = new object[] { "a", "b", 3 } };

        postProcessor.ProcessResult("key", "The first 3 letters: %s, %s and {%d}", args, "en", null)
            .Should().Be("The first 3 letters: a, b and {3}");
    }

    [Test]
    public void SprintfPostProcessor_WithoutArguments_ShouldReturnValue()
    {
        var postProcessor = new SprintfPostProcessor();

        postProcessor.Keyword.Should().Be("sprintf");
        postProcessor.ProcessTranslation("key", "%s", null, "en", null).Should().Be("%s");
        postProcessor.ProcessResult("key", "%s", null, "en", null).Should().Be("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object>(), "en", null).Should().Be("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object> { ["sprintf"] = null }, "en", null).Should().Be("%s");
        postProcessor.ProcessResult("key", "%s", new Dictionary<string, object> { ["sprintf"] = "a" }, "en", null).Should().Be("%s");
    }

    [TestCase(0, ExpectedResult = "none")]
    [TestCase(-5, ExpectedResult = "none")]
    [TestCase(1, ExpectedResult = "one")]
    [TestCase(3, ExpectedResult = "a few")]
    [TestCase(7, ExpectedResult = "many")]
    public string IntervalPostProcessor_ShouldPickMatchingInterval(int count)
    {
        var postProcessor = new IntervalPostProcessor();

        return postProcessor.ProcessResult("key", "(inf-0){none};(1){one};(2-5){a few};(6-inf){many};", new Dictionary<string, object> { ["count"] = count }, "en", null);
    }

    [Test]
    public void IntervalPostProcessor_NoMatch_ShouldReturnValueOrFirstInterval()
    {
        var postProcessor = new IntervalPostProcessor();
        const string value = "(1){one};(2-x){invalid};(y-inf){invalid};(inf-z){invalid};(z){invalid}";

        postProcessor.Keyword.Should().Be("interval");
        postProcessor.ProcessTranslation("key", value, null, "en", null).Should().Be(value);
        postProcessor.ProcessResult("key", value, new Dictionary<string, object> { ["count"] = 5 }, "en", null).Should().Be(value);

        postProcessor.UseFirstAsFallback = true;

        postProcessor.ProcessResult("key", value, null, "en", null).Should().Be("one");
        postProcessor.ProcessResult("key", "plain", null, "en", null).Should().Be("plain");
    }

    [Test]
    public void PseudoLocalizationPostProcessor_ConfiguredLanguage_ShouldReplaceAndRepeatLetters()
    {
        var options = new PseudoLocalizationOptions { LetterMultiplier = 3 };
        options.LanguagesToPseudo.Add("en");

        var postProcessor = new PseudoLocalizationPostProcessor(options);

        postProcessor.Keyword.Should().Be("pseudo");
        postProcessor.Options.Should().BeSameAs(options);
        postProcessor.ProcessTranslation("key", "abc", null, "en", null).Should().Be("abc");
        postProcessor.ProcessResult("key", "ab 1", null, "en", null).Should().Be("αααḅ 1");
        postProcessor.ProcessResult("key", "ab 1", null, "de", null).Should().Be("ab 1");
    }

    [Test]
    public void PseudoLocalizationOptions_InvalidValues_ShouldThrow()
    {
        var options = new PseudoLocalizationOptions();

        options.Invoking(o => o.LetterMultiplier = 0).Should().Throw<System.ArgumentOutOfRangeException>();
        options.Invoking(o => o.LetterMultiplier = 101).Should().Throw<System.ArgumentOutOfRangeException>();
        options.Invoking(o => o.RepeatedLetters = null).Should().Throw<System.ArgumentNullException>();
        options.Invoking(o => o.Letters = null).Should().Throw<System.ArgumentNullException>();

        options.RepeatedLetters = new[] { 'x' };
        options.Letters = new Dictionary<char, char> { ['x'] = 'y' };
        options.WrapStrings = true;

        options.RepeatedLetters.Should().Equal('x');
        options.Letters.Should().ContainKey('x');
        options.WrapStrings.Should().BeTrue();
    }

    [Test]
    public void Translator_PostProcessArgument_ShouldApplyMatchingPostProcessors()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "sprintf", "Hello %s");
        backend.AddTranslation("en", "translation", "interval", "(1){one item};(2-inf){many items};");

        var translator = new DefaultTranslator(backend);
        translator.PostProcessors.Add(new SprintfPostProcessor());
        translator.PostProcessors.Add(new IntervalPostProcessor());

        var i18Next = new I18NextNet(backend, translator) { Language = "en" };

        i18Next.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "World" } }).Should().Be("Hello World");
        i18Next.T("interval", new { postProcess = "interval", count = 1 }).Should().Be("one item");
        i18Next.T("interval", new { postProcess = new[] { "interval" }, count = 3 }).Should().Be("many items");
        i18Next.T("sprintf", new { postProcess = "interval, sprintf", sprintf = new[] { "You" } }).Should().Be("Hello You");
        i18Next.T("sprintf", new { postProcess = 5 }).Should().Be("Hello %s");

        translator.AllowPostprocessing = false;

        i18Next.T("sprintf", new { postProcess = "sprintf", sprintf = new[] { "World" } }).Should().Be("Hello %s");
    }
}
