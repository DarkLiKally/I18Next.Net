using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_DelimitersFixture
{
    private DefaultInterpolator _interpolator;

    [SetUp]
    public void SetUp()
    {
        _interpolator = new HtmlInterpolator(Substitute.For<ILogger>());
    }

    [Test]
    public void Delimiters_Defaults_ShouldMatchI18Next()
    {
        _interpolator.Prefix.Should().Be("{{");
        _interpolator.Suffix.Should().Be("}}");
        _interpolator.UnescapePrefix.Should().Be("-");
        _interpolator.NestingPrefix.Should().Be("$t(");
        _interpolator.NestingSuffix.Should().Be(")");
    }

    [Test]
    public async Task InterpolateAsync_CustomDelimiters_ShouldReplaceValues()
    {
        _interpolator.Prefix = "__";
        _interpolator.Suffix = "__";
        _interpolator.UnescapePrefix = "!";

        var args = new Dictionary<string, object> { ["name"] = "<b>", ["raw"] = "<i>" };

        var result = await _interpolator.InterpolateAsync("Hello __name__ and __!raw__, not {{name}}", "key", "en", args);

        result.Should().Be("Hello &lt;b&gt; and <i>, not {{name}}");
    }

    [Test]
    public async Task InterpolateAsync_RegexCharactersInDelimiters_ShouldBeEscaped()
    {
        _interpolator.Prefix = "[[";
        _interpolator.Suffix = "]]";

        var result = await _interpolator.InterpolateAsync("Value: [[value]]", "key", "en", new Dictionary<string, object> { ["value"] = 5 });

        result.Should().Be("Value: 5");
    }

    [Test]
    public async Task NestAsync_CustomNestingDelimiters_ShouldNest()
    {
        _interpolator.NestingPrefix = "@t{";
        _interpolator.NestingSuffix = "}";

        _interpolator.CanNest("Hello @t{other}").Should().BeTrue();
        _interpolator.CanNest("Hello $t(other)").Should().BeFalse();

        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello @t{other}").Should().BeTrue();

        var result = await _interpolator.NestAsync("Hello @t{other} $t(ignored)", "en", null, (_, key, _) => Task.FromResult($"<{key}>"));

        result.Should().Be("Hello <other> $t(ignored)");
    }

    [Test]
    public void Delimiters_Empty_ShouldThrow()
    {
        _interpolator.Invoking(i => i.Prefix = "").Should().Throw<ArgumentNullException>();
        _interpolator.Invoking(i => i.Suffix = null).Should().Throw<ArgumentNullException>();
        _interpolator.Invoking(i => i.UnescapePrefix = "").Should().Throw<ArgumentNullException>();
        _interpolator.Invoking(i => i.NestingPrefix = "").Should().Throw<ArgumentNullException>();
        _interpolator.Invoking(i => i.NestingSuffix = "").Should().Throw<ArgumentNullException>();
    }
}
