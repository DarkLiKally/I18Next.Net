using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Plugins;

// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_ChainedFormatsFixture
{
    public DefaultInterpolator_ChainedFormatsFixture()
    {
        _interpolator = new DefaultInterpolator(new TraceLogger());
        _interpolator.Formatters.Add(new UppercaseFormatter());
        _interpolator.Formatters.Add(new LowercaseFormatter());
        _interpolator.Formatters.Add(new MomentJsFormatter());

        _args = new Dictionary<string, object>
        {
            ["price"] = 1234.5,
            ["names"] = new[] { "anna", "ben" },
            ["days"] = -1,
            ["date"] = new DateTime(2018, 10, 2, 17, 7, 59),
            ["text"] = "Hello"
        };
    }
    private DefaultInterpolator _interpolator;
    private Dictionary<string, object> _args;


    private Task<string> InterpolateAsync(string source, string language = "en-US")
    {
        return _interpolator.InterpolateAsync(source, "key", language, _args);
    }

    [Theory]
    [InlineData("{{price, currency(EUR), uppercase}}", "€1,234.50")]
    [InlineData("{{names, list, uppercase}}", "ANNA AND BEN")]
    [InlineData("{{days, relativetime(numeric: auto), uppercase}}", "YESTERDAY")]
    [InlineData("{{text, uppercase, lowercase}}", "hello")]
    [InlineData("{{price, number(minimumFractionDigits: 2), uppercase}}", "1,234.50")]
    [InlineData("{{date, datetime(dateStyle: full), uppercase}}", "TUESDAY, OCTOBER 2, 2018")]
    public async Task InterpolateAsync_ChainedFormats_ShouldApplyFormatsInOrder(string source, string expected)
    {
        (await InterpolateAsync(source)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("{{price, #,##0.00}}", "1,234.50")]
    [InlineData("{{date, dddd, MMMM Do}}", "Tuesday, October 2nd")]
    [InlineData("{{date, dddd, MMMM}}", "Tuesday, October")]
    [InlineData("{{text, uppercase}}", "HELLO")]
    [InlineData("{{price, N1}}", "1,234.5")]
    public async Task InterpolateAsync_LegacyFormatsWithSeparator_ShouldNotBeChained(string source, string expected)
    {
        (await InterpolateAsync(source)).ShouldBe(expected);
    }

    [Fact]
    public async Task InterpolateAsync_CustomChainableFormat_ShouldBeChainedOnceRegistered()
    {
        _interpolator.Formatters.Add(new ReverseFormatter());

        (await InterpolateAsync("{{text, reverse}}")).ShouldBe("olleH");
        (await InterpolateAsync("{{text, uppercase, reverse}}")).ShouldBe("Hello");

        _interpolator.ChainableFormats.Add("reverse");

        (await InterpolateAsync("{{text, uppercase, reverse}}")).ShouldBe("OLLEH");
    }

    [Fact]
    public async Task InterpolateAsync_CustomSeparator_ShouldSplitChains()
    {
        _interpolator.FormatSeparator = "|";

        (await InterpolateAsync("{{names | list(type: disjunction) | uppercase}}")).ShouldBe("ANNA OR BEN");
    }

    private class ReverseFormatter : IFormatter
    {
        public bool CanFormat(object value, string format, string language)
        {
            return format == "reverse";
        }

        public string Format(object value, string format, string language)
        {
            var chars = value.ToString().ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }
}
