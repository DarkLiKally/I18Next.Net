using I18Next.Net.MachineTranslation.Internal;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class PlaceholderProtectorFixture
{
    [Fact]
    public void Protect_PlainText_ShouldOnlyEscapeMarkupCharacters()
    {
        var result = PlaceholderProtector.Xml.Protect("Tom & Jerry say 1 < 2 > 0");

        result.Text.ShouldBe("Tom &amp; Jerry say 1 &lt; 2 &gt; 0");
        result.Tokens.ShouldBeEmpty();
    }

    [Fact]
    public void Protect_Interpolations_ShouldBeWrappedInIgnoredElements()
    {
        var result = PlaceholderProtector.Xml.Protect("Hello {{name}}, you have {{value, number}} and {{- html}}");

        result.Text.ShouldBe(
            "Hello <x data-i=\"0\">{{name}}</x>, you have <x data-i=\"1\">{{value, number}}</x> and <x data-i=\"2\">{{- html}}</x>");
        result.Tokens.ShouldBe(["{{name}}", "{{value, number}}", "{{- html}}"]);
    }

    [Fact]
    public void Protect_Nesting_ShouldIncludeOptionsWithParenthesesAndQuotes()
    {
        var result = PlaceholderProtector.Xml.Protect("See $t(common:link, { \"label\": \"a (b)\", 'x': ')' }) now");

        result.Tokens.ShouldBe(["$t(common:link, { \"label\": \"a (b)\", 'x': ')' })"]);
        result.Text.ShouldStartWith("See <x data-i=\"0\">");
        result.Text.ShouldEndWith("</x> now");
    }

    [Fact]
    public void Protect_HtmlTags_ShouldBeProtectedAndEscaped()
    {
        var result = PlaceholderProtector.Html.Protect("Click <a href=\"/x?a=1&b=2\">here</a><br/> or <0>there</0>");

        result.Tokens.ShouldBe(["<a href=\"/x?a=1&b=2\">", "</a>", "<br/>", "<0>", "</0>"]);
        result.Text.ShouldBe("Click <span class=\"notranslate\" data-i=\"0\">&lt;a href=\"/x?a=1&amp;b=2\"&gt;</span>here"
                             + "<span class=\"notranslate\" data-i=\"1\">&lt;/a&gt;</span><span class=\"notranslate\" data-i=\"2\">&lt;br/&gt;</span> or "
                             + "<span class=\"notranslate\" data-i=\"3\">&lt;0&gt;</span>there<span class=\"notranslate\" data-i=\"4\">&lt;/0&gt;</span>");
    }

    [Theory]
    [InlineData("{{unclosed")]
    [InlineData("$t(unclosed")]
    [InlineData("a < b")]
    [InlineData("<unclosed")]
    [InlineData("<a <b>")]
    [InlineData("< >")]
    [InlineData("{single}")]
    [InlineData("$tx(key)")]
    public void Protect_IncompleteMarkers_ShouldStayText(string text)
    {
        var protector = PlaceholderProtector.Xml;
        var result = protector.Protect(text);

        protector.Restore(result.Text, result).ShouldBe(text);
    }

    [Fact]
    public void Protect_TagAfterIncompleteTag_ShouldBeProtected()
    {
        PlaceholderProtector.Xml.Protect("<a <b>").Tokens.ShouldBe(["<b>"]);
    }

    [Theory]
    [InlineData("Hello {{name}}!")]
    [InlineData("<b>{{count}}</b> items & $t(more, { \"count\": {{count}} })")]
    [InlineData("Line\nbreak with \"quotes\" and 'apostrophes'")]
    [InlineData("")]
    public void Restore_UnchangedText_ShouldRoundTrip(string text)
    {
        foreach (var protector in new[] { PlaceholderProtector.Xml, PlaceholderProtector.Html })
        {
            var result = protector.Protect(text);

            protector.Restore(result.Text, result).ShouldBe(text);
        }
    }

    [Fact]
    public void Restore_ReorderedAndChangedTokens_ShouldUseTheOriginalTokens()
    {
        var original = PlaceholderProtector.Xml.Protect("{{count}} items of {{name}}");

        PlaceholderProtector.Xml.Restore("<x data-i=\"1\">{{NAME}}</x> hat <x data-i=\"0\"></x> Artikel", original)
            .ShouldBe("{{name}} hat {{count}} Artikel");
    }

    [Fact]
    public void Restore_SelfClosingAndExtraAttributes_ShouldBeRecognized()
    {
        var original = PlaceholderProtector.Html.Protect("{{a}} {{b}}");

        PlaceholderProtector.Html.Restore("<span data-i=\"1\" class=\"notranslate\"/> <SPAN class=\"notranslate\" data-i=\"0\">x</SPAN>", original)
            .ShouldBe("{{b}} {{a}}");
    }

    [Fact]
    public void Restore_UnknownTokenIndex_ShouldBeDropped()
    {
        var original = PlaceholderProtector.Xml.Protect("{{a}}");

        PlaceholderProtector.Xml.Restore("<x data-i=\"7\">?</x>A<x data-i=\"99999999999\">?</x>", original).ShouldBe("A");
    }

    [Fact]
    public void Restore_Entities_ShouldBeDecoded()
    {
        var original = PlaceholderProtector.Html.Protect("Tom & Jerry");

        PlaceholderProtector.Html.Restore("Tom &amp; Jerry &lt;3 &#39;x&#39; &quot;y&quot;", original).ShouldBe("Tom & Jerry <3 'x' \"y\"");
    }

    [Theory]
    [InlineData("{{a}}", 0, 5)]
    [InlineData("x{{a}}", 1, 5)]
    [InlineData("$t(a)", 0, 5)]
    [InlineData("$t(a(b))c", 0, 8)]
    [InlineData("$t(a, { \"v\": \"\\\")\" })", 0, 21)]
    [InlineData("</b>", 0, 4)]
    [InlineData("x", 0, 0)]
    [InlineData("{", 0, 0)]
    [InlineData("$", 0, 0)]
    [InlineData("<", 0, 0)]
    [InlineData("</", 0, 0)]
    public void GetTokenLength_ShouldDetectTokens(string text, int index, int expected)
    {
        PlaceholderProtector.GetTokenLength(text, index).ShouldBe(expected);
    }
}
