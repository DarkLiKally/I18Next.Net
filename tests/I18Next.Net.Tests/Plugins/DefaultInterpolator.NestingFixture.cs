using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Internal;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_NestingFixture
{
    public DefaultInterpolator_NestingFixture()
    {
        var logger = Substitute.For<ILogger>();
        _interpolator = new DefaultInterpolator(logger);
    }
    private readonly DefaultInterpolator _interpolator;


    private Task<string> DummyTranslateAsync(string language, string key, IDictionary<string, object> args)
    {
        if (key == "anotherKeyA")
            return Task.FromResult("another value A");

        return key == "anotherKeyB" ? Task.FromResult("another value B") : Task.FromResult("translated dummy value");
    }

    [Fact]
    public void CanNest_WithMultipleNestingExpressionsUsingFastDetection_ShouldReturnTrue()
    {
        _interpolator.CanNest("Hello $t(testkey) and $t(testkey2)!").ShouldBeTrue();
    }

    [Fact]
    public void CanNest_WithMultipleNestingExpressionsUsingRegexDetection_ShouldReturnTrue()
    {
        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello $t(testkey) and $t(testkey2)!").ShouldBeTrue();
    }

    [Fact]
    public void CanNest_WithNoNestingExpression_ShouldReturnFalse()
    {
        _interpolator.CanNest("Hello World!").ShouldBeFalse();
    }

    [Fact]
    public void CanNest_WithOneNestingExpressionUsingFastDetection_ShouldReturnTrue()
    {
        _interpolator.CanNest("Hello $t(testkey)!").ShouldBeTrue();
    }

    [Fact]
    public void CanNest_WithOneNestingExpressionUsingRegexDetection_ShouldReturnTrue()
    {
        _interpolator.UseFastNestingMatch = false;
        _interpolator.CanNest("Hello $t(testkey)!").ShouldBeTrue();
    }

    [Fact]
    public async Task NestAsync_MultipleNestings_ShouldNestTheTranslation()
    {
        var result = await _interpolator.NestAsync("Hello $t(anotherKeyA) and $t(anotherKeyB)!", "en-US", null, DummyTranslateAsync);

        result.ShouldBe("Hello another value A and another value B!");
    }

    [Fact]
    public async Task NestAsync_NestingWithOnlyChildArgs_ShouldProvideChildArgs()
    {
        var result = await _interpolator.NestAsync("Hello $t(test, { \"arg2\": \"value2\" })!", "en-US", null, (language, key, args) =>
        {
            args.ShouldContainKey("arg2");
            args["arg2"].ShouldBe("value2");

            return Task.FromResult(args["arg2"].ToString());
        });

        result.ShouldBe("Hello value2!");
    }

    [Fact]
    public async Task NestAsync_NestingWithParentAndChildArgs_ShouldPassThroughArgsAndMerge()
    {
        var parentArgs = new { arg1 = "value1" };
        var result = await _interpolator.NestAsync("Hello $t(test, { \"arg2\": \"value2\" })!", "en-US", parentArgs.ToDictionary(),
            (language, key, args) =>
            {
                args.ShouldContainKey("arg1");
                args["arg1"].ShouldBe("value1");
                args.ShouldContainKey("arg2");
                args["arg2"].ShouldBe("value2");

                return Task.FromResult($"{args["arg1"]} - {args["arg2"]}");
            });

        result.ShouldBe("Hello value1 - value2!");
    }

    [Fact]
    public async Task NestAsync_OneDirectRecursiveNesting_ShouldPreventTheLoopAndDoNothing()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult("$t(test)"));

        result.ShouldBe("Hello $t(test)!");
    }

    [Fact]
    public async Task NestAsync_OneNesting_ShouldNestTheTranslation()
    {
        var result = await _interpolator.NestAsync("Hello $t(testkey)!", "en-US", null, DummyTranslateAsync);

        result.ShouldBe("Hello translated dummy value!");
    }

    [Fact]
    public async Task NestAsync_OneNestingWithMissingValue_ShouldReturnTheSource()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult((string)null));

        result.ShouldBe("Hello $t(test)!");
    }

    [Fact]
    public async Task NestAsync_OneNestingWithParentArgs_ShouldPassThroughArgs()
    {
        var parentArgs = new { arg1 = "value1" };
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", parentArgs.ToDictionary(), (language, key, args) =>
        {
            args.ShouldContainKey("arg1");
            args["arg1"].ShouldBe("value1");

            return Task.FromResult(args["arg1"].ToString());
        });

        result.ShouldBe("Hello value1!");
    }

    [Fact]
    public async Task NestAsync_OneRecursiveNesting_ShouldPreventTheLoopAndDoNothing()
    {
        var result = await _interpolator.NestAsync("Hello $t(test)!", "en-US", null, (language, key, args) => Task.FromResult("Oh no, $t(test)"));

        result.ShouldBe("Hello $t(test)!");
    }

    [Fact]
    public async Task NestAsync_TwoNestingsWithOneMissingValue_ShouldNestOneTokenAndIgnoreTheMissingOne()
    {
        var result = await _interpolator.NestAsync("Hello $t(test) $t(test2)!", "en-US", null, (language, key, args) =>
        {
            return key == "test" ? Task.FromResult((string)null) : Task.FromResult("some value");
        });

        result.ShouldBe("Hello $t(test) some value!");
    }
}
