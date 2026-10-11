using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Formatters;
using I18Next.Net.PolyglotJs;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

public class PolyglotInterpolatorFixture
{
    private readonly PolyglotInterpolator _interpolator = new();

    [Fact]
    public async Task InterpolateAsync_WithArguments_ShouldReplacePlaceholders()
    {
        var args = new Dictionary<string, object> { ["name"] = "Jane", ["city"] = "Berlin" };

        (await _interpolator.InterpolateAsync("Hello %{name} from %{city}, %{name}!", "key", "en", args)).ShouldBe("Hello Jane from Berlin, Jane!");
    }

    [Fact]
    public async Task InterpolateAsync_MissingArguments_ShouldKeepPlaceholders()
    {
        (await _interpolator.InterpolateAsync("Hello %{name}", "key", "en", null)).ShouldBe("Hello %{name}");
        (await _interpolator.InterpolateAsync("Hello %{name}", "key", "en", new Dictionary<string, object>())).ShouldBe("Hello %{name}");
        (await _interpolator.InterpolateAsync(null, "key", "en", null)).ShouldBeNull();
    }

    [Theory]
    [InlineData("en", 1, "1 car")]
    [InlineData("en", 2, "2 cars")]
    [InlineData("de", 0, "0 cars")]
    [InlineData("ja", 5, "5 car")]
    public async Task InterpolateAsync_SmartCount_ShouldChoosePluralForm(string language, int count, string expected)
    {
        var args = new Dictionary<string, object> { ["smart_count"] = count };

        (await _interpolator.InterpolateAsync("%{smart_count} car |||| %{smart_count} cars", "key", language, args)).ShouldBe(expected);
    }

    [Fact]
    public async Task InterpolateAsync_FormatArgument_ShouldFormatValue()
    {
        _interpolator.Formatters.Add(new UppercaseFormatter());
        var args = new Dictionary<string, object> { ["name"] = "Jane", ["name_format"] = "uppercase" };

        (await _interpolator.InterpolateAsync("Hello %{name}", "key", "en", args)).ShouldBe("Hello JANE");
    }

    [Fact]
    public void Nesting_ShouldNotBeSupported()
    {
        _interpolator.CanNest("$t(key)").ShouldBeFalse();
    }
}
