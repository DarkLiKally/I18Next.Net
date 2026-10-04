using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Formatters;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_ChainedFormatsFixture
{
    private DefaultInterpolator _interpolator;
    private Dictionary<string, object> _args;

    [SetUp]
    public void SetUp()
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

    private Task<string> InterpolateAsync(string source, string language = "en-US")
    {
        return _interpolator.InterpolateAsync(source, "key", language, _args);
    }

    [TestCase("{{price, currency(EUR), uppercase}}", ExpectedResult = "€1,234.50")]
    [TestCase("{{names, list, uppercase}}", ExpectedResult = "ANNA AND BEN")]
    [TestCase("{{days, relativetime(numeric: auto), uppercase}}", ExpectedResult = "YESTERDAY")]
    [TestCase("{{text, uppercase, lowercase}}", ExpectedResult = "hello")]
    [TestCase("{{price, number(minimumFractionDigits: 2), uppercase}}", ExpectedResult = "1,234.50")]
    [TestCase("{{date, datetime(dateStyle: long), uppercase}}", ExpectedResult = "TUESDAY, OCTOBER 2, 2018")]
    public async Task<string> InterpolateAsync_ChainedFormats_ShouldApplyFormatsInOrder(string source)
    {
        return await InterpolateAsync(source);
    }

    [TestCase("{{price, #,##0.00}}", ExpectedResult = "1,234.50")]
    [TestCase("{{date, dddd, MMMM Do}}", ExpectedResult = "Tuesday, October 2nd")]
    [TestCase("{{date, dddd, MMMM}}", ExpectedResult = "Tuesday, October")]
    [TestCase("{{text, uppercase}}", ExpectedResult = "HELLO")]
    [TestCase("{{price, N1}}", ExpectedResult = "1,234.5")]
    public async Task<string> InterpolateAsync_LegacyFormatsWithSeparator_ShouldNotBeChained(string source)
    {
        return await InterpolateAsync(source);
    }

    [Test]
    public async Task InterpolateAsync_CustomChainableFormat_ShouldBeChainedOnceRegistered()
    {
        _interpolator.Formatters.Add(new ReverseFormatter());

        (await InterpolateAsync("{{text, reverse}}")).Should().Be("olleH");
        (await InterpolateAsync("{{text, uppercase, reverse}}")).Should().Be("Hello");

        _interpolator.ChainableFormats.Add("reverse");

        (await InterpolateAsync("{{text, uppercase, reverse}}")).Should().Be("OLLEH");
    }

    [Test]
    public async Task InterpolateAsync_CustomSeparator_ShouldSplitChains()
    {
        _interpolator.FormatSeparator = "|";

        (await InterpolateAsync("{{names | list(type: disjunction) | uppercase}}")).Should().Be("ANNA OR BEN");
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
