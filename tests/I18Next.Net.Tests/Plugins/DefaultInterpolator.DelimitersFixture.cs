using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_DelimitersFixture
{
    public DefaultInterpolator_DelimitersFixture()
    {
        _interpolator = new HtmlInterpolator(Substitute.For<ILogger>());
    }
    private readonly DefaultInterpolator _interpolator;


    [Fact]
    public void Delimiters_Defaults_ShouldMatchI18Next()
    {
        _interpolator.Prefix.ShouldBe("{{");
        _interpolator.Suffix.ShouldBe("}}");
        _interpolator.UnescapePrefix.ShouldBe("-");
        _interpolator.NestingPrefix.ShouldBe("$t(");
        _interpolator.NestingSuffix.ShouldBe(")");
    }

    [Fact]
    public async Task InterpolateAsync_CustomDelimiters_ShouldReplaceValues()
    {
        _interpolator.Prefix = "__";
        _interpolator.Suffix = "__";
        _interpolator.UnescapePrefix = "!";

        var args = new Dictionary<string, object> { ["name"] = "<b>", ["raw"] = "<i>" };

        var result = await _interpolator.InterpolateAsync("Hello __name__ and __!raw__, not {{name}}", "key", "en", args);

        result.ShouldBe("Hello &lt;b&gt; and <i>, not {{name}}");
    }

    [Fact]
    public async Task InterpolateAsync_RegexCharactersInDelimiters_ShouldBeEscaped()
    {
        _interpolator.Prefix = "[[";
        _interpolator.Suffix = "]]";

        var result = await _interpolator.InterpolateAsync("Value: [[value]]", "key", "en", new Dictionary<string, object> { ["value"] = 5 });

        result.ShouldBe("Value: 5");
    }

    [Fact]
    public async Task NestAsync_CustomNestingDelimiters_ShouldNest()
    {
        _interpolator.NestingPrefix = "@t{";
        _interpolator.NestingSuffix = "}";

        _interpolator.CanNest("Hello @t{other}").ShouldBeTrue();
        _interpolator.CanNest("Hello $t(other)").ShouldBeFalse();

        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello @t{other}").ShouldBeTrue();

        var result = await _interpolator.NestAsync("Hello @t{other} $t(ignored)", "en", null, (_, key, _) => Task.FromResult($"<{key}>"));

        result.ShouldBe("Hello <other> $t(ignored)");
    }

    [Fact]
    public void Delimiters_Empty_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => _interpolator.Prefix = "");
        Should.Throw<ArgumentNullException>(() => _interpolator.Suffix = null);
        Should.Throw<ArgumentNullException>(() => _interpolator.UnescapePrefix = "");
        Should.Throw<ArgumentNullException>(() => _interpolator.NestingPrefix = "");
        Should.Throw<ArgumentNullException>(() => _interpolator.NestingSuffix = "");
    }
}
