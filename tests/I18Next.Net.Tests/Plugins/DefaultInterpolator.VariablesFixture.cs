using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_VariablesFixture
{
    private readonly DefaultInterpolator _interpolator = new(new TraceLogger());

    [Fact]
    public void Interpolate_ValueContainingPlaceholder_ShouldNotInterpolateValue()
    {
        var args = new Dictionary<string, object> { ["a"] = "{{b}}", ["b"] = "B" };

        _interpolator.Interpolate("{{a}} {{b}}", "key", "en", args).ShouldBe("{{b}} B");
    }

    [Fact]
    public void Interpolate_SkipOnVariablesDisabled_ShouldInterpolateValues()
    {
        _interpolator.SkipOnVariables = false;
        var args = new Dictionary<string, object> { ["a"] = "{{b}}", ["b"] = "B", ["loop"] = "{{loop}}" };

        _interpolator.Interpolate("{{a}} {{b}}", "key", "en", args).ShouldBe("B B");

        _interpolator.MaximumReplaces = 10;
        _interpolator.Interpolate("{{loop}}", "key", "en", args).ShouldBe("{{loop}}");
    }

    [Fact]
    public void Interpolate_UnescapedAndEscapedValues_ShouldBeHandledInOnePass()
    {
        var interpolator = new HtmlInterpolator(new TraceLogger());
        var args = new Dictionary<string, object> { ["html"] = "<b>" };

        interpolator.Interpolate("{{html}} {{- html}} {{-html}}", "key", "en", args).ShouldBe("&lt;b&gt; <b> <b>");
    }

    [Theory]
    [InlineData("{{a\nb}} {{a}}", "{{a\nb}} A")]
    [InlineData("{{}}}", "")]
    [InlineData("{{a}} {{", "A {{")]
    [InlineData("{{-}}", "")]
    [InlineData("no placeholders", "no placeholders")]
    public void Interpolate_EdgeCases_ShouldMatchRegexSemantics(string source, string expected)
    {
        _interpolator.Interpolate(source, "key", "en", new Dictionary<string, object> { ["a"] = "A" }).ShouldBe(expected);
    }

    [Fact]
    public void Interpolate_MissingValueHandler_ShouldReceiveTheMatch()
    {
        string matched = null;
        _interpolator.MissingValueHandler = (source, match) =>
        {
            matched = match.Value;

            return "?";
        };

        _interpolator.Interpolate("a {{missing, uppercase}} b", "key", "en", null).ShouldBe("a ? b");
        matched.ShouldBe("{{missing, uppercase}}");
    }

    [Fact]
    public void Interpolate_ManyCalls_ShouldReuseBuffersSafely()
    {
        for (var i = 0; i < 100; i++)
            _interpolator.Interpolate("{{a}}-{{b}}", "key", "en", new Dictionary<string, object> { ["a"] = i, ["b"] = new string('x', i * 20) })
                .ShouldBe($"{i}-{new string('x', i * 20)}");
    }

    [Fact]
    public void Interpolate_AlwaysFormat_ShouldPassValuesWithoutFormatToFormatters()
    {
        _interpolator.Formatters.Add(new UpperFormatter());

        _interpolator.Interpolate("{{a}}", "key", "en", new Dictionary<string, object> { ["a"] = "x" }).ShouldBe("x");

        _interpolator.AlwaysFormat = true;

        _interpolator.Interpolate("{{a}} {{b}}", "key", "en", new Dictionary<string, object> { ["a"] = "x" }).ShouldBe("X ");
    }

    [Fact]
    public async Task Translate_VariableContainingNesting_ShouldNotNest()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}");
        backend.AddTranslation("en", "translation", "both", "$t(secret) {{name}}");
        backend.AddTranslation("en", "translation", "secret", "Secret");

        var translator = new DefaultTranslator(backend, _interpolator);
        var options = new TranslationOptions { DefaultNamespace = "translation" };
        var args = new Dictionary<string, object> { ["name"] = "$t(secret)" };

        (await translator.TranslateAsync("en", "greeting", args, options)).ShouldBe("Hello $t(secret)");

        _interpolator.SkipOnVariables = false;

        (await translator.TranslateAsync("en", "greeting", args, options)).ShouldBe("Hello Secret");
        (await translator.TranslateAsync("en", "both", new Dictionary<string, object> { ["name"] = "x" }, options)).ShouldBe("Secret x");
    }

    [Fact]
    public async Task Translate_FallbackLanguageEqualsLanguage_ShouldRaiseMissingKeyOnce()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "key", "value");

        var translator = new DefaultTranslator(backend);
        var missing = new List<string>();
        translator.MissingKey += (_, e) => missing.Add($"{e.Language}:{e.Key}");

        var i18Next = new I18NextNet(backend, translator) { Language = "en" };
        i18Next.SetFallbackLanguages("en", "de");

        (await i18Next.Ta("missing")).ShouldBe("missing");
        i18Next.T("missing").ShouldBe("missing");

        missing.ShouldBe(["en:missing", "en:missing"]);
    }

    private class UpperFormatter : IFormatter
    {
        public bool CanFormat(object value, string format, string language)
        {
            return format == null && value is string;
        }

        public string Format(object value, string format, string language)
        {
            return ((string)value).ToUpperInvariant();
        }
    }
}
