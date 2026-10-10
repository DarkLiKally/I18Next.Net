using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class ChainedBackendFixture
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ITranslationBackend _backendA;
    private readonly ITranslationBackend _backendB;
    private readonly TestChainedBackend _backend;

    public ChainedBackendFixture()
    {
        _backendA = Substitute.For<ITranslationBackend>();
        _backendB = Substitute.For<ITranslationBackend>();

        _backendA.LoadNamespaceAsync("en", "backA").Returns(_ => Substitute.For<ITranslationTree>());
        _backendA.LoadNamespaceAsync("en", "backB").Returns((ITranslationTree)null);
        _backendB.LoadNamespaceAsync("en", "backB").Returns(_ => Substitute.For<ITranslationTree>());
        _backendB.LoadNamespaceAsync("en", "backA").Returns((ITranslationTree)null);

        _backend = new TestChainedBackend(_backendA, _backendB) { Now = Start };
    }

    [Fact]
    public async Task LoadNamespaceAsync_WithBackendANamespace_ShouldCallBackendA()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backA");

        tree.ShouldNotBeNull();

        await _backendA.Received(1).LoadNamespaceAsync("en", "backA");
        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backB");
    }

    [Fact]
    public async Task LoadNamespaceAsync_WithBackendBNamespace_ShouldCallBackendB()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backB");

        tree.ShouldNotBeNull();

        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendA.Received(1).LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.Received(1).LoadNamespaceAsync("en", "backB");
    }

    [Fact]
    public async Task LoadNamespaceAsync_NoBackendProvidesNamespace_ShouldReturnNull()
    {
        var backend = new ChainedBackend(_backendA);
        _backendA.LoadNamespaceAsync("en", "missing").Returns((ITranslationTree)null);

        (await backend.LoadNamespaceAsync("en", "missing")).ShouldBeNull();
    }

    [Fact]
    public void Constructor_NullBackends_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new ChainedBackend(null));
    }

    [Fact]
    public void Backends_ShouldReturnChainInOrder()
    {
        _backend.Backends.ShouldBe([_backendA, _backendB]);
    }

    [Fact]
    public async Task LoadNamespaceAsync_CacheDisabled_ShouldAskBackendsEveryTime()
    {
        await _backend.LoadNamespaceAsync("en", "backA");
        await _backend.LoadNamespaceAsync("en", "backA");

        await _backendA.Received(2).LoadNamespaceAsync("en", "backA");
    }

    [Fact]
    public async Task LoadNamespaceAsync_CacheEnabled_ShouldAskBackendsOnce()
    {
        _backend.CacheEnabled = true;

        var first = await _backend.LoadNamespaceAsync("en", "backA");
        _backend.Now = Start.AddYears(10);
        var second = await _backend.LoadNamespaceAsync("en", "backA");

        second.ShouldBeSameAs(first);
        await _backendA.Received(1).LoadNamespaceAsync("en", "backA");
    }

    [Fact]
    public async Task LoadNamespaceAsync_CacheEnabled_ShouldNotCacheMissingNamespaces()
    {
        _backend.CacheEnabled = true;
        _backendA.LoadNamespaceAsync("en", "missing").Returns((ITranslationTree)null);
        _backendB.LoadNamespaceAsync("en", "missing").Returns((ITranslationTree)null);

        (await _backend.LoadNamespaceAsync("en", "missing")).ShouldBeNull();
        (await _backend.LoadNamespaceAsync("en", "missing")).ShouldBeNull();

        await _backendA.Received(2).LoadNamespaceAsync("en", "missing");
    }

    [Fact]
    public async Task LoadNamespaceAsync_CacheExpired_ShouldReload()
    {
        _backend.CacheEnabled = true;
        _backend.CacheExpiration = TimeSpan.FromMinutes(5);

        var first = await _backend.LoadNamespaceAsync("en", "backA");

        _backend.Now = Start.AddMinutes(4);
        (await _backend.LoadNamespaceAsync("en", "backA")).ShouldBeSameAs(first);
        await _backendA.Received(1).LoadNamespaceAsync("en", "backA");

        _backend.Now = Start.AddMinutes(5);
        (await _backend.LoadNamespaceAsync("en", "backA")).ShouldNotBeSameAs(first);
        await _backendA.Received(2).LoadNamespaceAsync("en", "backA");
    }

    [Fact]
    public async Task LoadNamespaceAsync_ReloadThrows_ShouldReturnExpiredTree()
    {
        _backend.CacheEnabled = true;
        _backend.CacheExpiration = TimeSpan.FromMinutes(5);

        var first = await _backend.LoadNamespaceAsync("en", "backA");

        _backendA.LoadNamespaceAsync("en", "backA").ThrowsAsync(new InvalidOperationException("offline"));
        _backend.Now = Start.AddMinutes(10);

        (await _backend.LoadNamespaceAsync("en", "backA")).ShouldBeSameAs(first);
    }

    [Fact]
    public async Task LoadNamespaceAsync_ReloadReturnsNull_ShouldReturnExpiredTree()
    {
        _backend.CacheEnabled = true;
        _backend.CacheExpiration = TimeSpan.FromMinutes(5);

        var first = await _backend.LoadNamespaceAsync("en", "backA");

        _backendA.LoadNamespaceAsync("en", "backA").Returns((ITranslationTree)null);
        _backend.Now = Start.AddMinutes(10);

        (await _backend.LoadNamespaceAsync("en", "backA")).ShouldBeSameAs(first);
    }

    [Fact]
    public async Task LoadNamespaceAsync_ExpiredCacheOnFailureDisabled_ShouldPropagateFailures()
    {
        _backend.CacheEnabled = true;
        _backend.CacheExpiration = TimeSpan.FromMinutes(5);
        _backend.UseExpiredCacheOnFailure = false;

        await _backend.LoadNamespaceAsync("en", "backA");

        _backendA.LoadNamespaceAsync("en", "backA").ThrowsAsync(new InvalidOperationException("offline"));
        _backend.Now = Start.AddMinutes(10);

        await Should.ThrowAsync<InvalidOperationException>(() => _backend.LoadNamespaceAsync("en", "backA"));

        _backendA.LoadNamespaceAsync("en", "backA").Returns((ITranslationTree)null);

        (await _backend.LoadNamespaceAsync("en", "backA")).ShouldBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_FirstLoadThrows_ShouldPropagate()
    {
        _backend.CacheEnabled = true;
        _backendA.LoadNamespaceAsync("en", "backA").ThrowsAsync(new InvalidOperationException("offline"));

        await Should.ThrowAsync<InvalidOperationException>(() => _backend.LoadNamespaceAsync("en", "backA"));
    }

    [Fact]
    public async Task ClearCache_ShouldReloadNamespaces()
    {
        _backend.CacheEnabled = true;

        await _backend.LoadNamespaceAsync("en", "backA");
        await _backend.LoadNamespaceAsync("en", "backB");

        _backend.ClearCache("en", "backA");

        await _backend.LoadNamespaceAsync("en", "backA");
        await _backend.LoadNamespaceAsync("en", "backB");

        await _backendA.Received(2).LoadNamespaceAsync("en", "backA");
        await _backendB.Received(1).LoadNamespaceAsync("en", "backB");

        _backend.ClearCache();

        await _backend.LoadNamespaceAsync("en", "backB");

        await _backendB.Received(2).LoadNamespaceAsync("en", "backB");
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveToEarlierBackendsDisabled_ShouldNotSave()
    {
        var cache = CreateWritableBackend();
        var backend = new ChainedBackend(cache, _backendB);

        backend.SaveToEarlierBackends.ShouldBeFalse();

        (await backend.LoadNamespaceAsync("en", "backB")).ShouldNotBeNull();

        await cache.DidNotReceiveWithAnyArgs().SaveNamespaceAsync(null, null, null);
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveToEarlierBackends_ShouldSaveToEarlierWritableBackends()
    {
        var first = CreateWritableBackend();
        var second = CreateWritableBackend();
        var last = CreateWritableBackend();
        var backend = new ChainedBackend(first, _backendA, second, _backendB, last) { SaveToEarlierBackends = true };

        var tree = await backend.LoadNamespaceAsync("en", "backB");

        tree.ShouldNotBeNull();
        await first.Received(1).SaveNamespaceAsync("en", "backB", tree);
        await second.Received(1).SaveNamespaceAsync("en", "backB", tree);
        await last.DidNotReceiveWithAnyArgs().SaveNamespaceAsync(null, null, null);
        await last.DidNotReceiveWithAnyArgs().LoadNamespaceAsync(null, null);
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveToEarlierBackendsProvidedByFirstBackend_ShouldNotSave()
    {
        var cache = CreateWritableBackend();
        cache.LoadNamespaceAsync("en", "cached").Returns(_ => Substitute.For<ITranslationTree>());
        var source = CreateWritableBackend();
        var backend = new ChainedBackend(cache, source) { SaveToEarlierBackends = true };

        (await backend.LoadNamespaceAsync("en", "cached")).ShouldNotBeNull();

        await cache.DidNotReceiveWithAnyArgs().SaveNamespaceAsync(null, null, null);
        await source.DidNotReceiveWithAnyArgs().SaveNamespaceAsync(null, null, null);
        await source.DidNotReceiveWithAnyArgs().LoadNamespaceAsync(null, null);
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveToEarlierBackendsNoBackendProvidesNamespace_ShouldNotSave()
    {
        var cache = CreateWritableBackend();
        var backend = new ChainedBackend(cache, _backendA) { SaveToEarlierBackends = true };
        _backendA.LoadNamespaceAsync("en", "missing").Returns((ITranslationTree)null);

        (await backend.LoadNamespaceAsync("en", "missing")).ShouldBeNull();

        await cache.DidNotReceiveWithAnyArgs().SaveNamespaceAsync(null, null, null);
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveThrows_ShouldPropagate()
    {
        var cache = CreateWritableBackend();
        cache.SaveNamespaceAsync("en", "backB", Arg.Any<ITranslationTree>()).ThrowsAsync(new InvalidOperationException("offline"));
        var backend = new ChainedBackend(cache, _backendB) { SaveToEarlierBackends = true };

        await Should.ThrowAsync<InvalidOperationException>(() => backend.LoadNamespaceAsync("en", "backB"));
    }

    [Fact]
    public async Task LoadNamespaceAsync_SaveToEarlierInMemoryBackend_ShouldProvideNamespaceFromMemory()
    {
        var cache = new InMemoryBackend();
        var source = new SequenceBackend();
        var backend = new ChainedBackend(cache, source) { SaveToEarlierBackends = true };

        (await backend.LoadNamespaceAsync("de-AT", "translation")).GetValue("key", null).ShouldBe("value 1");
        (await backend.LoadNamespaceAsync("de-AT", "translation")).GetValue("key", null).ShouldBe("value 1");

        source.Loads.ShouldBe(1);
        cache.HasNamespace("de-AT", "translation").ShouldBeTrue();
        cache.HasNamespace("de", "translation").ShouldBeFalse();
    }

#pragma warning disable CS0618
    [Fact]
    public async Task CompositeBackend_ShouldBehaveLikeChainedBackend()
    {
        ChainedBackend backend = new CompositeBackend(_backendA, _backendB);

        (await backend.LoadNamespaceAsync("en", "backB")).ShouldNotBeNull();

        await _backendB.Received(1).LoadNamespaceAsync("en", "backB");
    }
#pragma warning restore CS0618

    [Fact]
    public async Task Translator_WithoutExpiration_ShouldLoadNamespaceOnce()
    {
        var source = new SequenceBackend();
        var translator = new TestTranslator(new ChainedBackend(source)) { Now = Start };

        (await Translate(translator)).ShouldBe("value 1");
        translator.Now = Start.AddYears(1);
        (await Translate(translator)).ShouldBe("value 1");

        source.Loads.ShouldBe(1);
    }

    [Fact]
    public async Task Translator_WithExpiration_ShouldReloadExpiredNamespaces()
    {
        var source = new SequenceBackend();
        var backend = new ChainedBackend(source) { CacheExpiration = TimeSpan.FromMinutes(5) };
        var translator = new TestTranslator(backend) { Now = Start };

        (await Translate(translator)).ShouldBe("value 1");

        translator.Now = Start.AddMinutes(4);
        (await Translate(translator)).ShouldBe("value 1");

        translator.Now = Start.AddMinutes(5);
        (await Translate(translator)).ShouldBe("value 2");

        translator.Now = Start.AddMinutes(9);
        (await Translate(translator)).ShouldBe("value 2");

        source.Loads.ShouldBe(2);
    }

    [Fact]
    public async Task Translator_ReloadThrows_ShouldKeepExpiredNamespaceUntilNextExpiry()
    {
        var source = new SequenceBackend();
        var backend = new ChainedBackend(source) { CacheExpiration = TimeSpan.FromMinutes(5) };
        var translator = new TestTranslator(backend) { Now = Start };

        (await Translate(translator)).ShouldBe("value 1");

        source.Fail = true;
        translator.Now = Start.AddMinutes(5);
        (await Translate(translator)).ShouldBe("value 1");

        translator.Now = Start.AddMinutes(6);
        (await Translate(translator)).ShouldBe("value 1");
        source.Loads.ShouldBe(2);

        source.Fail = false;
        translator.Now = Start.AddMinutes(10);
        (await Translate(translator)).ShouldBe("value 3");
    }

    [Fact]
    public async Task Translator_ClearCache_ShouldReloadNamespace()
    {
        var source = new SequenceBackend();
        var translator = new TestTranslator(source);

        (await Translate(translator)).ShouldBe("value 1");

        translator.ClearCache("en", "translation");

        (await Translate(translator)).ShouldBe("value 2");
    }

    private static IWritableTranslationBackend CreateWritableBackend()
    {
        var backend = Substitute.For<IWritableTranslationBackend>();
        backend.LoadNamespaceAsync(null, null).ReturnsForAnyArgs((ITranslationTree)null);

        return backend;
    }

    private static Task<string> Translate(ITranslator translator)
    {
        return translator.TranslateAsync("en", "key", new Dictionary<string, object>(), new TranslationOptions { DefaultNamespace = "translation" });
    }

    private class TestChainedBackend(params ITranslationBackend[] backends) : ChainedBackend(backends)
    {
        public DateTimeOffset Now { get; set; }

        protected override DateTimeOffset UtcNow => Now;
    }

    private class TestTranslator(ITranslationBackend backend) : DefaultTranslator(backend)
    {
        public DateTimeOffset Now { get; set; } = Start;

        protected override DateTimeOffset UtcNow => Now;
    }

    private class SequenceBackend : ITranslationBackend
    {
        public bool Fail { get; set; }

        public int Loads { get; private set; }

        public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
        {
            Loads++;

            if (Fail)
                throw new InvalidOperationException("offline");

            var tree = new DictionaryTranslationTree(@namespace) { ["key"] = $"value {Loads}" };

            return Task.FromResult<ITranslationTree>(tree);
        }
    }
}
