using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Plugins;

// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_NestedArgumentsFixture
{
    public DefaultInterpolator_NestedArgumentsFixture()
    {
        _interpolator = new DefaultInterpolator(Substitute.For<ILogger>());
    }
    private readonly DefaultInterpolator _interpolator;


    [Fact]
    public async Task NestAsync_DifferentArgumentTypes_ShouldBeConverted()
    {
        IDictionary<string, object> childArgs = null;

        await _interpolator.NestAsync("$t(test, { 'count': 2, 'ratio': 1.5, 'flag': true, 'off': false, 'none': null, 'list': [1, 'a'], 'obj': { 'name': 'x' } })",
            "en", null, (_, _, args) =>
            {
                childArgs = args;
                return Task.FromResult("done");
            });

        childArgs["count"].ShouldBe(2L);
        childArgs["ratio"].ShouldBe(1.5d);
        childArgs["flag"].ShouldBe(true);
        childArgs["off"].ShouldBe(false);
        childArgs["none"].ShouldBeNull();
        childArgs["list"].ShouldBeEquivalentTo(new object[] { 1L, "a" });
        childArgs["obj"].ShouldBeEquivalentTo(new Dictionary<string, object> { ["name"] = "x" });
    }

    [Fact]
    public async Task NestAsync_InterpolatedChildArguments_ShouldUseParentValues()
    {
        var parentArgs = new Dictionary<string, object> { ["amount"] = 3 };
        IDictionary<string, object> childArgs = null;

        await _interpolator.NestAsync("$t(test, { \"count\": {{amount}}, })", "en", parentArgs, (_, _, args) =>
        {
            childArgs = args;
            return Task.FromResult("done");
        });

        childArgs["count"].ShouldBe(3L);
        childArgs["amount"].ShouldBe(3);
    }

    [Fact]
    public void I18Next_NestingWithPluralArguments_ShouldTranslateNestedPlural()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "girlsAndBoys", "$t(girls, {\"count\": {{girls}} }) and {{count}} boy");
        backend.AddTranslation("en", "translation", "girlsAndBoys_plural", "$t(girls, {\"count\": {{girls}} }) and {{count}} boys");
        backend.AddTranslation("en", "translation", "girls", "{{count}} girl");
        backend.AddTranslation("en", "translation", "girls_plural", "{{count}} girls");

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

        i18Next.T("girlsAndBoys", new { count = 2, girls = 3 }).ShouldBe("3 girls and 2 boys");
    }

    [Fact]
    public async Task InterpolateAsync_UnescapedAndEscapedValues_ShouldReplaceBoth()
    {
        var result = await _interpolator.InterpolateAsync("{{-a}} {{b}} {{c.d}}", "key", "en",
            new Dictionary<string, object> { ["a"] = "<a>", ["b"] = "<b>", ["c"] = new Dictionary<string, object> { ["d"] = "e" } });

        result.ShouldBe("<a> <b> e");
    }
}
