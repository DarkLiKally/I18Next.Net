using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Extensions.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Extensions;

public class DistributedCacheBackendFixture
{
    private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task LoadNamespaceAsync_NotCached_ShouldReturnNull()
    {
        var backend = new DistributedCacheBackend(_cache);

        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveNamespaceAsync_HierarchicalTree_ShouldLoadSameValues()
    {
        var backend = new DistributedCacheBackend(_cache);
        var source = CreateTree("translation", ("greeting", "Grüß dich, {{name}}!"), ("menu.home", "Start"), ("menu.items.0", "\"Über\" <uns>"));

        await backend.SaveNamespaceAsync("de-AT", "translation", source);

        var tree = await backend.LoadNamespaceAsync("de-AT", "translation");

        tree.ShouldBeOfType<TranslationTree>();
        tree.Namespace.ShouldBe("translation");
        tree.GetAllValues().ShouldBe(source.GetAllValues(), true);
        ((IHierarchicalTranslationTree)tree).GetGroupValues("menu").Count.ShouldBe(2);
        (await backend.LoadNamespaceAsync("de", "translation")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveNamespaceAsync_ShouldStoreFlatJsonWithKeyPrefix()
    {
        var backend = new DistributedCacheBackend(_cache) { KeyPrefix = "app:" };

        await backend.SaveNamespaceAsync("en", "common", CreateTree("common", ("menu.home", "Home")));

        Encoding.UTF8.GetString(await _cache.GetAsync("app:en:common")).ShouldBe("""{"menu.home":"Home"}""");
        (await _cache.GetAsync(DistributedCacheBackend.DefaultKeyPrefix + "en:common")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveNamespaceAsync_NullValues_ShouldBeSkipped()
    {
        var backend = new DistributedCacheBackend(_cache);
        var source = Substitute.For<ITranslationTree>();
        source.GetAllValues().Returns(new Dictionary<string, string> { ["a"] = "A", ["b"] = null });

        await backend.SaveNamespaceAsync("en", "translation", source);

        (await backend.LoadNamespaceAsync("en", "translation")).GetAllValues().ShouldBe(new Dictionary<string, string> { ["a"] = "A" });
    }

    [Fact]
    public async Task LoadNamespaceAsync_FlatTreeBuilder_ShouldKeepKeys()
    {
        var backend = new DistributedCacheBackend(_cache, new GenericTranslationTreeBuilderFactory<FlatTranslationTreeBuilder>());
        await _cache.SetAsync("i18next:en:translation", Encoding.UTF8.GetBytes("""{"Hello. How are you?":"Hi","Hello":"Hello","count":1}"""));

        var tree = await backend.LoadNamespaceAsync("en", "translation");

        tree.ShouldBeOfType<DictionaryTranslationTree>();
        tree.Namespace.ShouldBe("translation");
        tree.GetAllValues().ShouldBe(new Dictionary<string, string> { ["Hello. How are you?"] = "Hi", ["Hello"] = "Hello" }, true);
    }

    [Fact]
    public async Task SaveNamespaceAsync_ShouldUseEntryOptions()
    {
        var cache = Substitute.For<IDistributedCache>();
        var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) };
        var backend = new DistributedCacheBackend(cache) { EntryOptions = options };

        await backend.SaveNamespaceAsync("en", "translation", CreateTree("translation", ("key", "value")));

        await cache.Received(1).SetAsync("i18next:en:translation", Arg.Any<byte[]>(), options, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void EntryOptions_Default_ShouldExpireAfterSevenDays()
    {
        var backend = new DistributedCacheBackend(_cache);

        backend.EntryOptions.AbsoluteExpirationRelativeToNow.ShouldBe(TimeSpan.FromDays(7));
        backend.KeyPrefix.ShouldBe("i18next:");
        backend.IgnoreCacheFailures.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveNamespaceAsync_ShouldRemoveCachedNamespace()
    {
        var backend = new DistributedCacheBackend(_cache);
        await backend.SaveNamespaceAsync("en", "translation", CreateTree("translation", ("key", "value")));
        await backend.SaveNamespaceAsync("en", "common", CreateTree("common", ("key", "value")));

        await backend.RemoveNamespaceAsync("en", "translation");

        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();
        (await backend.LoadNamespaceAsync("en", "common")).ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadNamespaceAsync_CacheFails_ShouldLogAndReturnNull()
    {
        var cache = Substitute.For<IDistributedCache>();
        var exception = new InvalidOperationException("offline");
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(exception);
        var logger = CreateLogger();
        var backend = new DistributedCacheBackend(cache) { Logger = logger };

        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();

        logger.Received(1).Log(LogLevel.Warning, exception, Arg.Any<string>(), Arg.Is<object[]>(a => a.Length == 2 && (string)a[0] == "en"));
    }

    [Fact]
    public async Task LoadNamespaceAsync_InvalidEntry_ShouldReturnNullWithoutLogger()
    {
        var backend = new DistributedCacheBackend(_cache);
        await _cache.SetAsync("i18next:en:translation", Encoding.UTF8.GetBytes("not json"));
        await _cache.SetAsync("i18next:en:common", Encoding.UTF8.GetBytes("[]"));

        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBeNull();
        (await backend.LoadNamespaceAsync("en", "common")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveNamespaceAsync_CacheFails_ShouldLogAndIgnore()
    {
        var cache = Substitute.For<IDistributedCache>();
        var exception = new InvalidOperationException("offline");
        cache.SetAsync(null, null, null).ReturnsForAnyArgs(Task.FromException(exception));
        var logger = CreateLogger();
        var backend = new DistributedCacheBackend(cache) { Logger = logger };

        await backend.SaveNamespaceAsync("en", "translation", CreateTree("translation", ("key", "value")));

        logger.Received(1).Log(LogLevel.Warning, exception, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task IgnoreCacheFailuresDisabled_ShouldThrow()
    {
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("offline"));
        cache.SetAsync(null, null, null).ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("offline")));
        var backend = new DistributedCacheBackend(cache) { IgnoreCacheFailures = false };

        await Should.ThrowAsync<InvalidOperationException>(() => backend.LoadNamespaceAsync("en", "translation"));
        await Should.ThrowAsync<InvalidOperationException>(() => backend.SaveNamespaceAsync("en", "translation", CreateTree("translation")));

        var invalidBackend = new DistributedCacheBackend(_cache) { IgnoreCacheFailures = false };
        await _cache.SetAsync("i18next:en:translation", Encoding.UTF8.GetBytes("not json"));

        await Should.ThrowAsync<Exception>(() => invalidBackend.LoadNamespaceAsync("en", "translation"));
    }

    [Fact]
    public async Task InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new DistributedCacheBackend(null));
        Should.Throw<ArgumentNullException>(() => new DistributedCacheBackend(_cache, null));

        await Should.ThrowAsync<ArgumentNullException>(() => new DistributedCacheBackend(_cache).SaveNamespaceAsync("en", "translation", null));
    }

    [Fact]
    public async Task ChainedBackend_SharedCache_ShouldLoadNamespaceFromSourceOnce()
    {
        var loads = 0;
        var source = new FuncBackend((language, ns) =>
        {
            loads++;

            return language == "fr" ? null : CreateTree(ns, ("greeting", "Hallo"));
        });

        var serverA = new ChainedBackend(new DistributedCacheBackend(_cache), source) { SaveToEarlierBackends = true };
        var serverB = new ChainedBackend(new DistributedCacheBackend(_cache), source) { SaveToEarlierBackends = true };

        (await serverA.LoadNamespaceAsync("de-AT", "translation")).GetValue("greeting", null).ShouldBe("Hallo");
        (await serverB.LoadNamespaceAsync("de-AT", "translation")).GetValue("greeting", null).ShouldBe("Hallo");

        loads.ShouldBe(1);
        (await _cache.GetAsync("i18next:de-AT:translation")).ShouldNotBeNull();
        (await serverB.LoadNamespaceAsync("fr", "translation")).ShouldBeNull();
        (await _cache.GetAsync("i18next:fr:translation")).ShouldBeNull();
    }

    [Fact]
    public void UseDistributedCache_ShouldPutCacheBeforeRegisteredBackend()
    {
        var source = new InMemoryBackend();
        source.AddTranslation("en", "translation", "greeting", "Hello");

        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(source)
            .UseDefaultLanguage("en")
            .UseDistributedCache(backend => backend.KeyPrefix = "app:"));

        using var provider = services.BuildServiceProvider();

        var chain = provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<ChainedBackend>();
        chain.SaveToEarlierBackends.ShouldBeTrue();
        chain.Backends.Count.ShouldBe(2);
        var cacheBackend = chain.Backends[0].ShouldBeOfType<DistributedCacheBackend>();
        cacheBackend.KeyPrefix.ShouldBe("app:");
        cacheBackend.Logger.ShouldBeOfType<TraceLogger>();
        chain.Backends[1].ShouldBeSameAs(source);

        provider.GetRequiredService<II18Next>().T("greeting").ShouldBe("Hello");
        provider.GetRequiredService<IDistributedCache>().Get("app:en:translation").ShouldNotBeNull();
    }

    [Fact]
    public void UseDistributedCache_WithoutConfiguration_ShouldUseDefaultBackend()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddI18NextLocalization(i18n => i18n.UseDistributedCache());

        using var provider = services.BuildServiceProvider();

        var chain = provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<ChainedBackend>();
        chain.Backends[0].ShouldBeOfType<DistributedCacheBackend>().KeyPrefix.ShouldBe(DistributedCacheBackend.DefaultKeyPrefix);
        chain.Backends[1].ShouldBeOfType<JsonFileBackend>();
    }

    private static ILogger CreateLogger()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return logger;
    }

    private static ITranslationTree CreateTree(string ns, params (string Key, string Value)[] values)
    {
        var builder = new HierarchicalTranslationTreeBuilder { Namespace = ns };

        foreach (var (key, value) in values)
            builder.AddTranslation(key, value);

        return builder.Build();
    }
}
