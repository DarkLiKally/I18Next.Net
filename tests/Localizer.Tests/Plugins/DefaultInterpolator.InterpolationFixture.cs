using System;
using System.Threading.Tasks;
using FluentAssertions;
using Localizer.Internal;
using Localizer.Logging;
using Localizer.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace Localizer.Tests.Plugins;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_InterpolationFixture
{
    [SetUp]
    public void SetUp()
    {
        _interpolator.MaximumReplaces = 1000;
        _interpolator.MissingValueHandler = null;
    }

    private DefaultInterpolator _interpolator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var logger = Substitute.For<ILogger>();
        _interpolator = new DefaultInterpolator(logger);
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndMissingArgs_ShouldReplaceThePlaceholderWithAnEmptyString()
    {
        var result = await _interpolator.InterpolateAsync("Hello {{name}}!", "testkey", "en-US", null);

        result.Should().Be("Hello !");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndMissingArgsWithMissingValueHandler_ShouldCallTheMissingValueHandler()
    {
        _interpolator.MissingValueHandler = (s, match) => "<missing>";

        var result = await _interpolator.InterpolateAsync("Hello {{name}}!", "testkey", "en-US", null);

        result.Should().Be("Hello <missing>!");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndMissingValue_ShouldReplaceThePlaceholderWithAnEmptyString()
    {
        var args = new { };
        var result = await _interpolator.InterpolateAsync("Hello {{name}}!", "testkey", "en-US", args.ToDictionary());

        result.Should().Be("Hello !");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndMissingValueWithMissingValueHandler_ShouldCallTheMissingValueHandler()
    {
        _interpolator.MissingValueHandler = (s, match) => "<missing>";

        var args = new { };
        var result = await _interpolator.InterpolateAsync("Hello {{name}}!", "testkey", "en-US", args.ToDictionary());

        _interpolator.MissingValueHandler = null;

        result.Should().Be("Hello <missing>!");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndNestedKey_ShouldReplaceThePlaceholder()
    {
        var args = new { person = new { name = "John Doe" } };
        var result = await _interpolator.InterpolateAsync("Hello {{person.name}}!", "testkey", "en-US", args.ToDictionary());

        result.Should().Be("Hello John Doe!");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholderAndNestedMissingValue_ShouldReplaceThePlaceholderWithAnEmptyString()
    {
        var args = new { };
        var result = await _interpolator.InterpolateAsync("Hello {{person.informations.name}}!", "testkey", "en-US", args.ToDictionary());

        result.Should().Be("Hello !");
    }

    [Test]
    public async Task InterpolateAsync_PlaceholdersWithFormat_ShouldReplaceThePlaceholdersAndFormatTheValueUsingTheDefaultFormatter()
    {
        var args = new { date = new DateTime(2018, 02, 01), number = 33.24 };
        var result = await _interpolator.InterpolateAsync("The value {{number, 000.0000}} happened on {{date, MM/dd/yyyy}}.", "testkey", "en-US",
            args.ToDictionary());

        result.Should().Be("The value 033.2400 happened on 02/01/2018.");
    }

    [Test]
    public async Task InterpolateAsync_SimpleStringWithoutPlaceholders_ShouldReturnTheString()
    {
        var result = await _interpolator.InterpolateAsync("Simple string", "testkey", "en-US", null);

        result.Should().Be("Simple string");
    }

    [Test]
    public async Task InterpolateAsync_StringWithMultiplePlaceholders_ShouldReplaceThePlaceholders()
    {
        var args = new { name = "World", from = "outer space" };

        var result = await _interpolator.InterpolateAsync("Hello {{name}}! Greetings from {{from}}.", "testkey", "en-US", args.ToDictionary());
        result.Should().Be("Hello World! Greetings from outer space.");
    }

    [Test]
    public async Task InterpolateAsync_StringWithMultiplePlaceholdersAndMaxReplacesTwo_ShouldReplaceMaxTwoPlaceholders()
    {
        var args = new { name = "World", from = "outer space" };
        _interpolator.MaximumReplaces = 2;

        var result = await _interpolator.InterpolateAsync("Hello {{name}}! Greetings from {{from}} to {{name}}.", "testkey", "en-US",
            args.ToDictionary());
        result.Should().Be("Hello World! Greetings from outer space to {{name}}.");
    }

    [Test]
    public async Task InterpolateAsync_StringWithOnePlaceholder_ShouldReplaceThePlaceholder()
    {
        var args = new { name = "World" };
        var result = await _interpolator.InterpolateAsync("Hello {{name}}!", "testkey", "en-US", args.ToDictionary());

        result.Should().Be("Hello World!");
    }

    [Test]
    public async Task InterpolateAsync_StringWithOnePlaceholderAndWhitespace_ShouldReplaceThePlaceholder()
    {
        var args = new { name = "World" };

        var result = await _interpolator.InterpolateAsync("Hello {{ name }}!", "testkey", "en-US", args.ToDictionary());
        result.Should().Be("Hello World!");

        result = await _interpolator.InterpolateAsync("Hello {{ name}}!", "testkey", "en-US", args.ToDictionary());
        result.Should().Be("Hello World!");

        result = await _interpolator.InterpolateAsync("Hello {{name }}!", "testkey", "en-US", args.ToDictionary());
        result.Should().Be("Hello World!");
    }
}
