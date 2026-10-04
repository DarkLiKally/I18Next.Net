using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class HtmlInterpolatorFixture
{
    private HtmlInterpolator _interpolator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _interpolator = new HtmlInterpolator(Substitute.For<ILogger>());
    }

    [Test]
    public async Task InterpolateAsync_EscapedValue_ShouldEncodeHtml()
    {
        var args = new Dictionary<string, object> { ["value"] = "<a href=\"/x\">'&'</a>" };

        var result = await _interpolator.InterpolateAsync("Value: {{value}}", "key", "en", args);

        result.Should().Be("Value: &lt;a href=&quot;&#x2F;x&quot;&gt;&#39;&amp;&#39;&lt;&#x2F;a&gt;");
    }

    [Test]
    public async Task InterpolateAsync_UnescapedValue_ShouldNotEncodeHtml()
    {
        var args = new Dictionary<string, object> { ["value"] = "<b>" };

        var result = await _interpolator.InterpolateAsync("Value: {{- value}}", "key", "en", args);

        result.Should().Be("Value: <b>");
    }

    [Test]
    public async Task InterpolateAsync_EscapingDisabled_ShouldNotEncodeHtml()
    {
        var interpolator = new HtmlInterpolator(Substitute.For<ILogger>()) { EscapeValues = false };

        var result = await interpolator.InterpolateAsync("{{value}}", "key", "en", new Dictionary<string, object> { ["value"] = "<b>" });

        result.Should().Be("<b>");
    }
}
