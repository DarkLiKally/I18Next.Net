using System.Linq;

using I18Next.Net.Tool.Extraction;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Extraction;

public class MarkupKeyExtractorFixture
{
    private readonly ExtractionOptions _options = new();

    [Fact]
    public void Extract_RazorView_ShouldFindCallsAndIndexers()
    {
        const string view = """
                            @inject II18Next I18n
                            <h1>@I18n.T("title")</h1>
                            <p title="@I18n.T("common:tooltip")">@await I18n.Ta("body", new { name = Model.Name })</p>
                            <span>@I18n.T("items", new { count = Model.Items.Count(i => i.Visible) })</span>
                            <span>@I18n.T("place", new { count = 1, ordinal = true, context = "male" })</span>
                            <span>@Localizer["welcome"] @HtmlLocalizer["rows", new { count = 3 }]</span>
                            @{ var menu = I18n.TObject("menu"); var model = I18n.T<Model>("model"); }
                            @if (File.Exists("x.json") && I18n.Exists("exists")) { }
                            <span>@I18n.T("quote \"x\"")</span>
                            <span>@I18n.T(key)</span>
                            """;

        var keys = new MarkupKeyExtractor(_options).Extract(view, "View.cshtml").ToList();

        keys.Select(k => (k.Namespace, k.Key, k.HasCount, k.Ordinal, k.Context, k.ReturnsObject)).ShouldBe([
            ("translation", "title", false, false, null, false),
            ("common", "tooltip", false, false, null, false),
            ("translation", "body", false, false, null, false),
            ("translation", "items", true, false, null, false),
            ("translation", "place", true, true, "male", false),
            ("translation", "menu", false, false, null, true),
            ("translation", "model", false, false, null, true),
            ("translation", "exists", false, false, null, false),
            ("translation", "quote \"x\"", false, false, null, false),
            ("translation", "welcome", false, false, null, false),
            ("translation", "rows", true, false, null, false)
        ]);
        keys[0].Line.ShouldBe(2);
        keys[0].File.ShouldBe("View.cshtml");
    }

    [Fact]
    public void Extract_CustomNames_ShouldBeUsed()
    {
        _options.FunctionNames.Add("Tr");
        _options.LocalizerNames.Add("L");

        new MarkupKeyExtractor(_options).Extract("@Tr(\"a\") @L[\"b\"] @Other(\"c\") @ST(\"d\")", "Component.razor").Select(k => k.Key).ShouldBe(["a", "b"]);
    }

    [Fact]
    public void Extract_UnclosedCall_ShouldIgnoreTheArguments()
    {
        var key = new MarkupKeyExtractor(_options).Extract("@I18n.T(\"a\", new { count = 1 ", "View.cshtml").ShouldHaveSingleItem();

        key.HasCount.ShouldBeFalse();
    }
}
