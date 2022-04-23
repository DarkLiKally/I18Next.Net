using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_NestingFixture
{
    private DefaultInterpolator _interpolator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _interpolator = new DefaultInterpolator();
    }

    private Task<string> DummyTranslateAsync(string language, string key, IDictionary<string, object> args)
    {
        if (key == "anotherKeyA")
            return Task.FromResult("another value A");

        if (key == "anotherKeyB")
            return Task.FromResult("another value B");

        return Task.FromResult("translated dummy value");
    }

    [Test]
    public void CanNest_WithMultipleNestingExpressionsUsingFastDetection_ShouldReturnTrue()
    {
        _interpolator.CanNest("Hello $t(testkey) and $t(testkey2)!").Should().BeTrue();
    }

    [Test]
    public void CanNest_WithMultipleNestingExpressionsUsingRegexDetection_ShouldReturnTrue()
    {
        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello $t(testkey) and $t(testkey2)!").Should().BeTrue();
    }

    [Test]
    public void CanNest_WithNoNestingExpression_ShouldReturnFalse()
    {
        _interpolator.CanNest("Hello World!").Should().BeFalse();
    }

    [Test]
    public void CanNest_WithOneNestingExpressionUsingFastDetection_ShouldReturnTrue()
    {
        _interpolator.CanNest("Hello $t(testkey)!").Should().BeTrue();
    }

    [Test]
    public void CanNest_WithOneNestingExpressionUsingRegexDetection_ShouldReturnTrue()
    {
        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello $t(testkey)!").Should().BeTrue();
    }

    [Test]
    public async Task NestAsync_MultipleNestings_ShouldNestTheTranslation()
    {
        var result = await _interpolator.NestAsync("Hello $t(anotherKeyA) and $t(anotherKeyB)!", "en-US", null, DummyTranslateAsync);

        result.Should().Be("Hello another value A and another value B!");
    }

    [Test]
    public async Task NestAsync_NestingWithOnlyChildArgs_ShouldProvideChildArgs()
    {
        var result = await _interpolator.NestAsync("Hello $t(test, { \"arg2\": \"value2\" })!", "en-US", null, (language, key, args) =>
        {
            args.Should().ContainKey("arg2");
            args["arg2"].Should().Be("value2");

            return Task.FromResult(args["arg2"].ToString());
        });

        result.Should().Be("Hello value2!");
    }

    [Test]
    public async Task NestAsync_NestingWithParentAndChildArgs_ShouldPassThroughArgsAndMerge()
    {
        var parentArgs = new { arg1 = "value1" };
        var result = await _interpolator.NestAsync("Hello $t(test, { \"arg2\": \"value2\" })!", "en-US", parentArgs.ToDictionary(),
            (language, key, args) =>
            {
                args.Should().ContainKey("arg1");
                args["arg1"].Should().Be("value1");
                args.Should().ContainKey("arg2");
                args["arg2"].Should().Be("value2");

                return Task.FromResult($"{args["arg1"]} - {args["arg2"]}");
            });

        result.Should().Be("Hello value1 - value2!");
    }

    [Test]
    public async Task NestAsync_OneDirectRecursiveNesting_ShouldPreventTheLoopAndDoNothing()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult("$t(test)"));

        result.Should().Be("Hello $t(test)!");
    }

    [Test]
    public async Task NestAsync_OneNesting_ShouldNestTheTranslation()
    {
        var result = await _interpolator.NestAsync("Hello $t(testkey)!", "en-US", null, DummyTranslateAsync);

        result.Should().Be("Hello translated dummy value!");
    }

    [Test]
    public async Task NestAsync_OneNestingWithMissingValue_ShouldReturnTheSource()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult((string) null));

        result.Should().Be("Hello $t(test)!");
    }

    [Test]
    public async Task NestAsync_OneNestingWithParentArgs_ShouldPassThroughArgs()
    {
        var parentArgs = new { arg1 = "value1" };
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", parentArgs.ToDictionary(), (language, key, args) =>
        {
            args.Should().ContainKey("arg1");
            args["arg1"].Should().Be("value1");

            return Task.FromResult(args["arg1"].ToString());
        });

        result.Should().Be("Hello value1!");
    }

    [Test]
    public async Task NestAsync_OneRecursiveNesting_ShouldPreventTheLoopAndDoNothing()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult("Oh no, $t(test)"));

        result.Should().Be("Hello $t(test)!");
    }

    [Test]
    public async Task NestAsync_TwoNestingsWithOneMissingValue_ShouldNestOneTokenAndIgnoreTheMissingOne()
    {
        var result = await _interpolator.NestAsync("Hello $t(test) $t(test2)!", "en-US", null, (language, key, args) =>
        {
            if (key == "test")
                return Task.FromResult((string) null);

            return Task.FromResult("some value");
        });

        result.Should().Be("Hello $t(test) some value!");
    }
}