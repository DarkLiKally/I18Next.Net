using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class DefaultInterpolator_NestedArgumentsFixture
{
    private DefaultInterpolator _interpolator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _interpolator = new DefaultInterpolator(Substitute.For<ILogger>());
    }

    [Test]
    public async Task NestAsync_DifferentArgumentTypes_ShouldBeConverted()
    {
        IDictionary<string, object> childArgs = null;

        await _interpolator.NestAsync("$t(test, { 'count': 2, 'ratio': 1.5, 'flag': true, 'off': false, 'none': null, 'list': [1, 'a'], 'obj': { 'name': 'x' } })",
            "en", null, (_, _, args) =>
            {
                childArgs = args;
                return Task.FromResult("done");
            });

        childArgs["count"].Should().Be(2L);
        childArgs["ratio"].Should().Be(1.5d);
        childArgs["flag"].Should().Be(true);
        childArgs["off"].Should().Be(false);
        childArgs["none"].Should().BeNull();
        childArgs["list"].Should().BeEquivalentTo(new object[] { 1L, "a" });
        childArgs["obj"].Should().BeEquivalentTo(new Dictionary<string, object> { ["name"] = "x" });
    }

    [Test]
    public async Task NestAsync_InterpolatedChildArguments_ShouldUseParentValues()
    {
        var parentArgs = new Dictionary<string, object> { ["amount"] = 3 };
        IDictionary<string, object> childArgs = null;

        await _interpolator.NestAsync("$t(test, { \"count\": {{amount}}, })", "en", parentArgs, (_, _, args) =>
        {
            childArgs = args;
            return Task.FromResult("done");
        });

        childArgs["count"].Should().Be(3L);
        childArgs["amount"].Should().Be(3);
    }

    [Test]
    public void I18Next_NestingWithPluralArguments_ShouldTranslateNestedPlural()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "girlsAndBoys", "$t(girls, {\"count\": {{girls}} }) and {{count}} boy");
        backend.AddTranslation("en", "translation", "girlsAndBoys_plural", "$t(girls, {\"count\": {{girls}} }) and {{count}} boys");
        backend.AddTranslation("en", "translation", "girls", "{{count}} girl");
        backend.AddTranslation("en", "translation", "girls_plural", "{{count}} girls");

        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

        i18Next.T("girlsAndBoys", new { count = 2, girls = 3 }).Should().Be("3 girls and 2 boys");
    }

    [Test]
    public async Task InterpolateAsync_UnescapedAndEscapedValues_ShouldReplaceBoth()
    {
        var result = await _interpolator.InterpolateAsync("{{-a}} {{b}} {{c.d}}", "key", "en",
            new Dictionary<string, object> { ["a"] = "<a>", ["b"] = "<b>", ["c"] = new Dictionary<string, object> { ["d"] = "e" } });

        result.Should().Be("<a> <b> e");
    }
}
