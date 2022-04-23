using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

namespace I18Next.Net.Tests.Plugins;

[TestFixture]
public class DefaultTranslatorFixture
{
    [SetUp]
    public void SetUp()
    {
        _translator = new DefaultTranslator(_backend, null, _pluralResolver, _interpolator);
        _options = new TranslationOptions { DefaultNamespace = "test" };
    }

    [TearDown]
    public void TearDown()
    {
        _backend.ClearReceivedCalls();
        _pluralResolver.ClearReceivedCalls();
        _interpolator.ClearReceivedCalls();
        _translationTree.ClearSubstitute();
    }

    private ITranslationBackend _backend;
    private IPluralResolver _pluralResolver;
    private IInterpolator _interpolator;
    private DefaultTranslator _translator;
    private ITranslationTree _translationTree;
    private TranslationOptions _options;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _backend = Substitute.For<ITranslationBackend>();
        _pluralResolver = Substitute.For<IPluralResolver>();
        _interpolator = Substitute.For<IInterpolator>();
        _translationTree = Substitute.For<ITranslationTree>();

        _pluralResolver.GetPluralSuffix("en-US", 0).Returns("_0");
        _pluralResolver.GetPluralSuffix("en-US", 1).Returns("_1");
        _pluralResolver.GetPluralSuffix("en-US", 2).Returns("_2");
        _pluralResolver.GetPluralSuffix("en-US", 3).Returns("_3");
        _pluralResolver.GetPluralSuffix("en-US", 4).Returns("_4");
        _pluralResolver.GetPluralSuffix("en-US", 5).Returns("_5");
        _pluralResolver.GetPluralSuffix("ja-JP", 0).Returns("_0");
        _pluralResolver.GetPluralSuffix("ja-JP", 1).Returns("_0");
        _pluralResolver.GetPluralSuffix("ja-JP", 2).Returns("_0");
        _pluralResolver.NeedsPlural("en-US").Returns(true);
        _pluralResolver.NeedsPlural("ja-JP").Returns(true);
        _backend.LoadNamespaceAsync("en-US", "test").Returns(_translationTree);
        _interpolator.InterpolateAsync(null, null, null, null).ReturnsForAnyArgs(c => c.ArgAt<string>(0));
        _interpolator.NestAsync(null, null, null, null).ReturnsForAnyArgs(c => c.ArgAt<string>(0));
    }

    [Test]
    public async Task TranslateAsync_CallMultiplePostProcessors_ShouldApplyPostProcessorsInOrder()
    {
        var postProcessor1 = Substitute.For<IPostProcessor>();
        postProcessor1.Process("test", "translated", Arg.Any<IDictionary<string, object>>()).Returns("post-processed1");
        postProcessor1.Keyword.Returns("testProcess1");
        var postProcessor2 = Substitute.For<IPostProcessor>();
        postProcessor2.Keyword.Returns("testProcess2");
        var postProcessor3 = Substitute.For<IPostProcessor>();
        postProcessor3.Process("test", "post-processed1", Arg.Any<IDictionary<string, object>>()).Returns("post-processed3");
        postProcessor3.Keyword.Returns("testProcess3");

        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.PostProcessors.Add(postProcessor1);
        _translator.PostProcessors.Add(postProcessor2);
        _translator.PostProcessors.Add(postProcessor3);

        var args = new { postProcess = new[] { "testProcess1", "testProcess3" } };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("post-processed3");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor1.Received(1).Process("test", "translated", Arg.Any<IDictionary<string, object>>());
        postProcessor2.ReceivedWithAnyArgs(0).Process(null, null, null);
        postProcessor3.Received(1).Process("test", "post-processed1", Arg.Any<IDictionary<string, object>>());
    }

    [Test]
    public async Task TranslateAsync_CustomPostProcessor_ShouldApplyPostProcessing()
    {
        var postProcessor = Substitute.For<IPostProcessor>();
        postProcessor.Process("test", "translated", Arg.Any<IDictionary<string, object>>()).Returns("post-processed");
        postProcessor.Keyword.Returns("testProcess");

        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.PostProcessors.Add(postProcessor);

        var args = new { postProcess = "testProcess" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("post-processed");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor.Received(1).Process("test", "translated", Arg.Any<IDictionary<string, object>>());
    }

    [Test]
    public async Task TranslateAsync_DisableInterpolation_ShouldTranslateWithoutInterpolation()
    {
        _translationTree.GetValue("test", null).Returns("translated");
        _translator.AllowInterpolation = false;

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_DisableInterpolationInArgs_ShouldTranslateWithoutInterpolation()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.AllowInterpolation = true;

        var args = new { interpolate = false };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_DisableNesting_ShouldTranslateWithoutNesting()
    {
        _translationTree.GetValue("test", null).Returns("translated");
        _translator.AllowNesting = false;

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_DisableNestingInArgs_ShouldTranslateWithoutNesting()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.AllowNesting = true;

        var args = new { nest = false };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_DisablePostProcessing_ShouldTranslateWithoutPostProcessing()
    {
        var postProcessor = Substitute.For<IPostProcessor>();
        postProcessor.Process("test", "translated", null).Returns("post-processed");

        _translationTree.GetValue("test", null).Returns("translated");
        _translator.PostProcessors.Add(postProcessor);
        _translator.AllowPostprocessing = false;

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor.Received(0).Process("test", "translated", null);
    }

    [Test]
    public async Task TranslateAsync_DisablePostProcessingInArgs_ShouldTranslateWithoutPostProcessing()
    {
        var postProcessor = Substitute.For<IPostProcessor>();
        postProcessor.Process("test", "translated", null).Returns("post-processed");

        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.PostProcessors.Add(postProcessor);
        _translator.AllowPostprocessing = true;

        var args = new { applyPostProcessor = false };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor.Received(0).Process("test", "translated", null);
    }

    [Test]
    public async Task TranslateAsync_MultipleCustomPostProcessorWithDifferentKeys_ShouldApplyOnlyTheSpecifiedPostProcessors()
    {
        var postProcessor1 = Substitute.For<IPostProcessor>();
        postProcessor1.Process("test", "translated", Arg.Any<IDictionary<string, object>>()).Returns("post-processed1");
        postProcessor1.Keyword.Returns("testProcess1");
        var postProcessor2 = Substitute.For<IPostProcessor>();
        postProcessor2.Process("test", "post-processed1", Arg.Any<IDictionary<string, object>>()).Returns("post-processed2");
        postProcessor2.Keyword.Returns("testProcess2");

        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.PostProcessors.Add(postProcessor1);
        _translator.PostProcessors.Add(postProcessor2);

        var args = new { postProcess = "testProcess1" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("post-processed1");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor1.Received(1).Process("test", "translated", Arg.Any<IDictionary<string, object>>());
        postProcessor2.ReceivedWithAnyArgs(0).Process(null, null, null);
    }

    [Test]
    public async Task TranslateAsync_MultipleCustomPostProcessorWithSameKey_ShouldApplyPostProcessorsInOrder()
    {
        var postProcessor1 = Substitute.For<IPostProcessor>();
        postProcessor1.Process("test", "translated", Arg.Any<IDictionary<string, object>>()).Returns("post-processed1");
        postProcessor1.Keyword.Returns("testProcess");
        var postProcessor2 = Substitute.For<IPostProcessor>();
        postProcessor2.Process("test", "post-processed1", Arg.Any<IDictionary<string, object>>()).Returns("post-processed2");
        postProcessor2.Keyword.Returns("testProcess");

        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translator.PostProcessors.Add(postProcessor1);
        _translator.PostProcessors.Add(postProcessor2);

        var args = new { postProcess = "testProcess" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("post-processed2");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
        postProcessor1.Received(1).Process("test", "translated", Arg.Any<IDictionary<string, object>>());
        postProcessor2.Received(1).Process("test", "post-processed1", Arg.Any<IDictionary<string, object>>());
    }

    [Test]
    public async Task TranslateAsync_ReplaceArgsInSubObject_ShouldUseSubObject()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var replaceArgs = new { value = "test" };
        var args = new { replace = replaceArgs };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.Received(1)
            .InterpolateAsync("translated", "test", "en-US",
                Verify.That<IDictionary<string, object>>(d => d.Should().BeEquivalentTo(replaceArgs.ToDictionary())));
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_TwoTimesTheSameTree_ShouldOnlyLoadTheTreeOnce()
    {
        _translationTree.GetValue("test", null).Returns("translated");

        var result1 = await _translator.TranslateAsync("en-US", "test", null, _options);
        var result2 = await _translator.TranslateAsync("en-US", "test", null, _options);

        result1.Should().Be("translated");
        result2.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(2).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(2).InterpolateAsync(null, null, null, null);
        _interpolator.Received(2).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_UsingCiMode_ShouldReturnNamespaceAndKey()
    {
        _translationTree.GetValue("test", null).Returns("translated");
        _options.DefaultNamespace = "testns";

        var result = await _translator.TranslateAsync("cimode", "testkey", null, _options);

        result.Should().Be("testns:testkey");

        await _backend.ReceivedWithAnyArgs(0).LoadNamespaceAsync("en-US", "test");
        _translationTree.ReceivedWithAnyArgs(0).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.ReceivedWithAnyArgs(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithContext_ShouldDoContextHandling()
    {
        _translationTree.GetValue("test_male", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var args = new { context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test_male", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithContextButNoTranslation_ShouldUseFallback()
    {
        _translationTree.GetValue("test_male", Arg.Any<IDictionary<string, object>>()).Returns((string) null);
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var args = new { context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test_male", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithCount_ShouldDoPluralHandling()
    {
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var args = new { count = 2 };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountAndContext_ShouldDoPluralAndContextHandling()
    {
        _translationTree.GetValue("test_male_2", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var args = new { count = 2, context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test_male_2", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountAndContextButNoContextTranslation_ShouldUsePluralFallback()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("wrong-translated");
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translationTree.GetValue("test_male", Arg.Any<IDictionary<string, object>>()).Returns((string) null);
        _translationTree.GetValue("test_male_2", Arg.Any<IDictionary<string, object>>()).Returns((string) null);

        var args = new { count = 2, context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(0).GetValue("test", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountAndContextButNoPluralAndContextTranslation_ShouldUseNormalFallback()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns((string) null);
        _translationTree.GetValue("test_male", Arg.Any<IDictionary<string, object>>()).Returns((string) null);
        _translationTree.GetValue("test_male_2", Arg.Any<IDictionary<string, object>>()).Returns((string) null);

        var args = new { count = 2, context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountAndContextButOnlyContextTranslation_ShouldUseContextFallback()
    {
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("wrong-translated");
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns("wrong-translated");
        _translationTree.GetValue("test_male", Arg.Any<IDictionary<string, object>>()).Returns("translated");
        _translationTree.GetValue("test_male_2", Arg.Any<IDictionary<string, object>>()).Returns((string) null);

        var args = new { count = 2, context = "male" };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(0).GetValue("test", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_male_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(0).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountButNoTranslation_ShouldUseFallback()
    {
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns((string) null);
        _translationTree.GetValue("test", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        var args = new { count = 2 };
        var result = await _translator.TranslateAsync("en-US", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithCountButNoTranslation_ShouldUseFallbackPluralRules()
    {
        var jpTranslationTree = Substitute.For<ITranslationTree>();
        _backend.LoadNamespaceAsync("ja-JP", "test").Returns(jpTranslationTree);
        jpTranslationTree.GetValue(null, null).ReturnsForAnyArgs((string) null);
        _translationTree.GetValue("test_2", Arg.Any<IDictionary<string, object>>()).Returns("translated");

        _options.FallbackLanguages = new[] { "en-US" };
        var args = new { count = 2 };
        var result = await _translator.TranslateAsync("ja-JP", "test", args.ToDictionary(), _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("ja-JP", "test");
        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        jpTranslationTree.Received(1).GetValue("test_0", Arg.Any<IDictionary<string, object>>());
        jpTranslationTree.DidNotReceive().GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.Received(1).GetValue("test_2", Arg.Any<IDictionary<string, object>>());
        _translationTree.DidNotReceive().GetValue("test_0", Arg.Any<IDictionary<string, object>>());
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.Received(1).GetPluralSuffix("ja-JP", 2);
        _pluralResolver.Received(1).GetPluralSuffix("en-US", 2);
        _pluralResolver.Received(1).NeedsPlural("en-US");
    }

    [Test]
    public async Task TranslateAsync_WithDefaultNsAndSimpleString_ShouldTranslate()
    {
        _translationTree.GetValue("test", null).Returns("translated");

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKey_ShouldRaiseMissingKeyEvent()
    {
        _translationTree.GetValue("test", null).Returns((string) null);

        var missingKeyCalls = 0;

        _translator.MissingKey += (sender, args) =>
        {
            args.Key.Should().Be("test");
            args.Namespace.Should().Be("test");
            args.Language.Should().Be("en-US");
            args.PossibleKeys.Should().BeEquivalentTo("test");
            missingKeyCalls++;
        };

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("test");
        missingKeyCalls.Should().Be(1);

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKeyAndContext_ShouldRaiseMissingKeyEventWithPossibleContextKeys()
    {
        var arguments = new Dictionary<string, object>
        {
            { "context", "ctx" }
        };

        _translationTree.GetValue("test", arguments).Returns((string) null);
        _translationTree.GetValue("test_ctx", arguments).Returns((string) null);

        var missingKeyCalls = 0;

        _translator.MissingKey += (sender, args) =>
        {
            args.Key.Should().Be("test");
            args.Namespace.Should().Be("test");
            args.Language.Should().Be("en-US");
            args.PossibleKeys.Should().BeEquivalentTo("test_ctx", "test");
            missingKeyCalls++;
        };

        var result = await _translator.TranslateAsync("en-US", "test", arguments, _options);

        result.Should().Be("test");
        missingKeyCalls.Should().Be(1);

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", arguments);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKeyAndContextAndPlural_ShouldRaiseMissingKeyEventWithPossibleContextAndPluralKeys()
    {
        var arguments = new Dictionary<string, object>
        {
            { "context", "ctx" },
            { "count", 2 }
        };

        _translationTree.GetValue("test", arguments).Returns((string) null);
        _translationTree.GetValue("test_2", arguments).Returns((string) null);
        _translationTree.GetValue("test_ctx", arguments).Returns((string) null);
        _translationTree.GetValue("test_ctx_2", arguments).Returns((string) null);

        var missingKeyCalls = 0;

        _translator.MissingKey += (sender, args) =>
        {
            args.Key.Should().Be("test");
            args.Namespace.Should().Be("test");
            args.Language.Should().Be("en-US");
            args.PossibleKeys.Should().BeEquivalentTo("test_ctx_2", "test_ctx", "test_2", "test");
            missingKeyCalls++;
        };
        var result = await _translator.TranslateAsync("en-US", "test", arguments, _options);

        result.Should().Be("test");
        missingKeyCalls.Should().Be(1);

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", arguments);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(1).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(1).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKeyAndMultipleMissingKeyHandlers_ShouldCallMissingKeyHandlers()
    {
        _translationTree.GetValue("test", null).Returns((string) null);

        var missingKeyHandlerA = Substitute.For<IMissingKeyHandler>();
        missingKeyHandlerA
            .HandleMissingKeyAsync(_translator,
                Arg.Is<MissingKeyEventArgs>(x => x.Key == "test" && x.Language == "en-US" && x.Namespace == "test"))
            .Returns(Task.CompletedTask);

        var missingKeyHandlerB = Substitute.For<IMissingKeyHandler>();
        missingKeyHandlerA
            .HandleMissingKeyAsync(_translator,
                Arg.Is<MissingKeyEventArgs>(x => x.Key == "test" && x.Language == "en-US" && x.Namespace == "test"))
            .Returns(Task.CompletedTask);

        _translator.MissingKeyHandlers.Add(missingKeyHandlerA);
        _translator.MissingKeyHandlers.Add(missingKeyHandlerB);

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("test");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await missingKeyHandlerA.Received(1).HandleMissingKeyAsync(_translator, Arg.Any<MissingKeyEventArgs>());
        await missingKeyHandlerB.Received(1).HandleMissingKeyAsync(_translator, Arg.Any<MissingKeyEventArgs>());
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKeyAndOneMissingKeyHandler_ShouldCallMissingKeyHandler()
    {
        _translationTree.GetValue("test", null).Returns((string) null);

        var missingKeyHandler = Substitute.For<IMissingKeyHandler>();
        missingKeyHandler
            .HandleMissingKeyAsync(_translator,
                Arg.Is<MissingKeyEventArgs>(x => x.Key == "test" && x.Language == "en-US" && x.Namespace == "test"))
            .Returns(Task.CompletedTask);

        _translator.MissingKeyHandlers.Add(missingKeyHandler);

        var result = await _translator.TranslateAsync("en-US", "test", null, _options);

        result.Should().Be("test");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await missingKeyHandler.Received(1).HandleMissingKeyAsync(_translator, Arg.Any<MissingKeyEventArgs>());
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithMissingKeyAndPlural_ShouldRaiseMissingKeyEventWithPossiblePluralKeys()
    {
        var arguments = new Dictionary<string, object>
        {
            { "count", 2 }
        };

        _translationTree.GetValue("test", arguments).Returns((string) null);
        _translationTree.GetValue("test_2", arguments).Returns((string) null);

        var missingKeyCalls = 0;

        _translator.MissingKey += (sender, args) =>
        {
            args.Key.Should().Be("test");
            args.Namespace.Should().Be("test");
            args.Language.Should().Be("en-US");
            args.PossibleKeys.Should().BeEquivalentTo("test_2", "test");
            missingKeyCalls++;
        };

        var result = await _translator.TranslateAsync("en-US", "test", arguments, _options);

        result.Should().Be("test");
        missingKeyCalls.Should().Be(1);

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", arguments);
        await _interpolator.ReceivedWithAnyArgs(0).InterpolateAsync(null, null, null, null);
        _interpolator.Received(0).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(1).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(1).NeedsPlural(null);
    }

    [Test]
    public async Task TranslateAsync_WithOtherThanDefaultNsAndSimpleString_ShouldTranslateUsingTheSpecifiedNamespace()
    {
        _translationTree.GetValue("test", null).Returns("translated");
        _options.DefaultNamespace = "other";

        var result = await _translator.TranslateAsync("en-US", "test:test", null, _options);

        result.Should().Be("translated");

        await _backend.Received(1).LoadNamespaceAsync("en-US", "test");
        _translationTree.Received(1).GetValue("test", null);
        await _interpolator.ReceivedWithAnyArgs(1).InterpolateAsync(null, null, null, null);
        _interpolator.Received(1).CanNest("translated");
        await _interpolator.ReceivedWithAnyArgs(0).NestAsync(null, null, null, null);
        _pluralResolver.ReceivedWithAnyArgs(0).GetPluralSuffix(null, 0);
        _pluralResolver.ReceivedWithAnyArgs(0).NeedsPlural(null);
    }
}