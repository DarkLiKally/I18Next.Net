using System.Collections.Generic;
using System.Threading.Tasks;

using Bunit;

using I18Next.Net.Backends;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace I18Next.Net.Blazor.Tests;

public class TransFixture : BunitContext
{
    private static readonly Dictionary<string, RenderFragment<RenderFragment>> Components = new()
    {
        ["link"] = content => builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "href", "/terms");
            builder.AddContent(2, content);
            builder.CloseElement();
        },
        ["icon"] = _ => builder => builder.AddMarkupContent(0, "<svg></svg>")
    };

    public TransFixture()
    {
        var backend = TestServices.CreateBackend();

        backend.AddTranslation("en", "translation", "html", "Hello <strong>{{name}}</strong>!");
        backend.AddTranslation("en", "translation", "lines", "One<br>Two<br/>Three<BR />Four");
        backend.AddTranslation("en", "translation", "attributes", "<a href=\"https://example.com\">Click</a> <b class=\"x\">bold</b> <img/>");
        backend.AddTranslation("en", "translation", "nested", "<p><b>Bold <i>both</i></b> plain</p>");
        backend.AddTranslation("en", "translation", "unmatched", "a </b> b <i>c");
        backend.AddTranslation("en", "translation", "crossed", "<b>a <i>b</b> c</i>");
        backend.AddTranslation("en", "translation", "entities", "Tom &amp; Jerry &lt;3");
        backend.AddTranslation("en", "translation", "upper", "<STRONG>Up</STRONG>");
        backend.AddTranslation("en", "translation", "terms", "Read the <link>terms</link> and <icon/>.");
        backend.AddTranslation("en", "translation", "linkInBold", "<b>Go <link>home</link></b>");
        backend.AddTranslation("en", "common", "markup", "Save <b>now</b>");

        Services.AddTestI18Next(backend);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_ShouldRenderTranslation()
    {
        var cut = Render<Trans>(p => p.Add(c => c.Key, "title"));

        cut.Markup.ShouldBe("Welcome");
    }

    [Fact]
    public void Render_Args_ShouldInterpolate()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "items")
            .Add(c => c.Args, new { count = 3 }));

        cut.Markup.ShouldBe("3 items");
    }

    [Fact]
    public void Render_Ns_ShouldUseNamespace()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "save")
            .Add(c => c.Ns, "common"));

        cut.Markup.ShouldBe("Save");
    }

    [Fact]
    public void Render_Markup_ShouldBeEscapedByDefault()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "html")
            .Add(c => c.Args, new { name = "Jane" }));

        cut.FindAll("strong").Count.ShouldBe(0);
        cut.MarkupMatches("Hello &lt;strong&gt;Jane&lt;/strong&gt;!");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderAllowedTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "html")
            .Add(c => c.Args, new { name = "Jane" })
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("Hello <strong>Jane</strong>!");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderScriptsInArgsAsText()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "html")
            .Add(c => c.Args, new { name = "<script>alert(1)</script>" })
            .Add(c => c.AllowHtml, true));

        cut.FindAll("script").Count.ShouldBe(0);
        cut.Find("strong").TextContent.ShouldBe("<script>alert(1)</script>");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderTagsWithAttributesAndUnknownTagsAsText()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "attributes")
            .Add(c => c.AllowHtml, true));

        cut.FindAll("a, b, img").Count.ShouldBe(0);
        cut.Markup.ShouldContain("&lt;a href=");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderVoidAndSelfClosingTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "lines")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("One<br>Two<br>Three<br>Four");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderNestedTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "nested")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("<p><b>Bold <i>both</i></b> plain</p>");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderUnmatchedClosingTagsAsTextAndCloseOpenTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "unmatched")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("a &lt;/b&gt; b <i>c</i>");
    }

    [Fact]
    public void Render_AllowHtml_ShouldCloseInnerTagsWithOuterTag()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "crossed")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("<b>a <i>b</i></b> c&lt;/i&gt;");
    }

    [Fact]
    public void Render_AllowHtml_ShouldDecodeEntities()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "entities")
            .Add(c => c.AllowHtml, true));

        cut.Nodes[0].TextContent.ShouldBe("Tom & Jerry <3");
    }

    [Fact]
    public void Render_AllowHtml_ShouldRenderTagsLowercase()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "upper")
            .Add(c => c.AllowHtml, true));

        cut.Markup.ShouldBe("<strong>Up</strong>");
    }

    [Fact]
    public void Render_AllowHtmlWithNs_ShouldRenderAllowedTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "markup")
            .Add(c => c.Ns, "common")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("Save <b>now</b>");
    }

    [Fact]
    public void Render_ConfiguredAllowedTags_ShouldOnlyRenderThem()
    {
        Services.Configure<I18NextBlazorOptions>(o => o.AllowedHtmlTags = new HashSet<string> { "i" });

        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "nested")
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("&lt;p&gt;&lt;b&gt;Bold <i>both</i>&lt;/b&gt; plain&lt;/p&gt;");
    }

    [Fact]
    public void Render_Components_ShouldRenderNamedTags()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "terms")
            .Add(c => c.Components, Components));

        cut.MarkupMatches("Read the <a href=\"/terms\">terms</a> and <svg></svg>.");
    }

    [Fact]
    public void Render_ComponentsWithoutAllowHtml_ShouldRenderHtmlTagsAsText()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "linkInBold")
            .Add(c => c.Components, Components));

        cut.MarkupMatches("&lt;b&gt;Go <a href=\"/terms\">home</a>&lt;/b&gt;");
    }

    [Fact]
    public void Render_ComponentsWithAllowHtml_ShouldRenderBoth()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "linkInBold")
            .Add(c => c.Components, Components)
            .Add(c => c.AllowHtml, true));

        cut.MarkupMatches("<b>Go <a href=\"/terms\">home</a></b>");
    }

    [Fact]
    public void Render_EmptyComponents_ShouldRenderText()
    {
        var cut = Render<Trans>(p => p
            .Add(c => c.Key, "terms")
            .Add(c => c.Components, new Dictionary<string, RenderFragment<RenderFragment>>()));

        cut.MarkupMatches("Read the &lt;link&gt;terms&lt;/link&gt; and &lt;icon/&gt;.");
    }

    [Fact]
    public async Task Render_LanguageChanged_ShouldRenderAgain()
    {
        var cut = Render<Trans>(p => p.Add(c => c.Key, "title"));
        var i18n = Services.GetRequiredService<IBlazorI18Next>();

        await cut.InvokeAsync(() => i18n.ChangeLanguageAsync("de"));

        cut.Markup.ShouldBe("Willkommen");
    }

    [Fact]
    public void Render_TranslationsChanged_ShouldRenderAgain()
    {
        var inner = new InMemoryBackend();
        inner.AddTranslation("en", "translation", "title", "Before");
        var backend = new NotifyingBackend(inner);
        Services.AddTestI18Next(backend);
        var cut = Render<Trans>(p => p.Add(c => c.Key, "title"));

        inner.RemoveNamespace("en", "translation");
        inner.AddTranslation("en", "translation", "title", "After");
        backend.RaiseTranslationsChanged("en", "translation");

        cut.WaitForAssertion(() => cut.Markup.ShouldBe("After"));
    }
}
