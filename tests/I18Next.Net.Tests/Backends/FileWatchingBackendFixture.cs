using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class FileWatchingBackendFixture : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public FileWatchingBackendFixture()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "en"));
        Directory.CreateDirectory(Path.Combine(_directory, "de"));
        File.WriteAllText(Path.Combine(_directory, "en", "translation.json"), """{ "greeting": "Hello" }""");
        File.WriteAllText(Path.Combine(_directory, "de", "translation.json"), """{ "greeting": "Hallo" }""");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Fact]
    public async Task ChangedFile_ShouldBeTranslatedAgain()
    {
        using var backend = new FileWatchingBackend(new JsonFileBackend(_directory), _directory) { Delay = TimeSpan.FromMilliseconds(50) };
        var i18Next = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "de-AT" };

        i18Next.T("greeting").ShouldBe("Hallo");

        var change = WaitForChange(backend);
        File.WriteAllText(Path.Combine(_directory, "de", "translation.json"), """{ "greeting": "Servus" }""");

        var args = await change;

        args.Language.ShouldBe("de");
        args.Namespace.ShouldBe("translation");
        i18Next.T("greeting").ShouldBe("Servus");
    }

    [Fact]
    public async Task FileOutsideOfLayout_ShouldReloadAll()
    {
        using var backend = new FileWatchingBackend(new JsonFileBackend(_directory), _directory) { Delay = TimeSpan.FromMilliseconds(50) };

        var change = WaitForChange(backend);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{}");

        var args = await change;

        args.Language.ShouldBeNull();
        args.Namespace.ShouldBeNull();
    }

    [Fact]
    public async Task RenamedFile_ShouldNotifyBothNamespaces()
    {
        using var backend = new FileWatchingBackend(new JsonFileBackend(_directory), _directory) { Delay = TimeSpan.FromMilliseconds(100) };
        var changes = new List<TranslationsChangedEventArgs>();
        var completion = new TaskCompletionSource<bool>();

        backend.TranslationsChanged += (_, e) =>
        {
            lock (changes)
            {
                changes.Add(e);

                if (changes.Exists(c => c.Namespace == "translation") && changes.Exists(c => c.Namespace == "common"))
                    completion.TrySetResult(true);
            }
        };

        File.Move(Path.Combine(_directory, "en", "translation.json"), Path.Combine(_directory, "en", "common.json"));

        (await Task.WhenAny(completion.Task, Task.Delay(Timeout))).ShouldBe(completion.Task);
    }

    [Fact]
    public async Task InnerNotifyingBackend_ShouldBeForwarded()
    {
        var inner = Substitute.For<INotifyingTranslationBackend>();
        var tree = Substitute.For<ITranslationTree>();
        inner.LoadNamespaceAsync("en", "translation").Returns(tree);

        using var backend = new FileWatchingBackend(inner, _directory);
        TranslationsChangedEventArgs received = null;
        backend.TranslationsChanged += (_, e) => received = e;

        inner.TranslationsChanged += Raise.EventWith(inner, new TranslationsChangedEventArgs("en", null));

        received.Language.ShouldBe("en");
        backend.Backend.ShouldBe(inner);
        (await backend.LoadNamespaceAsync("en", "translation")).ShouldBe(tree);
    }

    [Fact]
    public void Constructor_NullArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new FileWatchingBackend(null, _directory));
        Should.Throw<ArgumentNullException>(() => new FileWatchingBackend(new InMemoryBackend(), null));
    }

    [Fact]
    public async Task ChainedBackend_ShouldClearCacheAndForwardChanges()
    {
        var inner = Substitute.For<INotifyingTranslationBackend>();
        inner.LoadNamespaceAsync("de", "translation").Returns(_ => Substitute.For<ITranslationTree>());

        var backend = new ChainedBackend(inner, new InMemoryBackend()) { CacheEnabled = true };
        TranslationsChangedEventArgs received = null;
        backend.TranslationsChanged += (_, e) => received = e;

        var first = await backend.LoadNamespaceAsync("de", "translation");
        (await backend.LoadNamespaceAsync("de", "translation")).ShouldBe(first);

        inner.TranslationsChanged += Raise.EventWith(inner, new TranslationsChangedEventArgs("de", "translation"));

        received.ShouldNotBeNull();
        (await backend.LoadNamespaceAsync("de", "translation")).ShouldNotBe(first);
    }

    [Theory]
    [InlineData(null, null, "de-AT", "translation", true)]
    [InlineData("de", null, "de-AT", "common", true)]
    [InlineData("de", "translation", "de", "translation", true)]
    [InlineData("DE", "translation", "de-at", "translation", true)]
    [InlineData("de", "translation", "de", "common", false)]
    [InlineData("de", "translation", "en", "translation", false)]
    [InlineData("de", "translation", "den-X", "translation", false)]
    [InlineData("de-AT", "translation", "de", "translation", false)]
    [InlineData("de-AT", "translation", "de-CH", "translation", false)]
    public void Affects_ShouldMatchLanguagesAndNamespaces(string changedLanguage, string changedNamespace, string language, string ns, bool expected)
    {
        new TranslationsChangedEventArgs(changedLanguage, changedNamespace).Affects(language, ns).ShouldBe(expected);
    }

    [Fact]
    public async Task WatchTranslationFiles_ShouldWrapRegisteredBackend()
    {
        var services = new ServiceCollection();
        services.AddI18NextLocalization(i18n => i18n
            .AddBackend(new JsonFileBackend(_directory))
            .UseDefaultLanguage("en")
            .WatchTranslationFiles(_directory));

        using (var provider = services.BuildServiceProvider())
        {
            var backend = provider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<FileWatchingBackend>();
            backend.Backend.ShouldBeOfType<JsonFileBackend>();
            backend.Delay = TimeSpan.FromMilliseconds(50);

            var i18Next = provider.GetRequiredService<II18Next>();
            i18Next.T("greeting").ShouldBe("Hello");

            var change = WaitForChange(backend);
            File.WriteAllText(Path.Combine(_directory, "en", "translation.json"), """{ "greeting": "Hi" }""");
            await change;

            i18Next.T("greeting").ShouldBe("Hi");
        }

        var typeServices = new ServiceCollection();
        typeServices.AddI18NextLocalization(i18n => i18n.AddBackend<InMemoryBackend>().WatchTranslationFiles(_directory));
        using var typeProvider = typeServices.BuildServiceProvider();
        typeProvider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<FileWatchingBackend>().Backend.ShouldBeOfType<InMemoryBackend>();

        var factoryServices = new ServiceCollection();
        factoryServices.AddI18NextLocalization(i18n => i18n.AddBackend(_ => new InMemoryBackend()).WatchTranslationFiles(_directory));
        using var factoryProvider = factoryServices.BuildServiceProvider();
        factoryProvider.GetRequiredService<ITranslationBackend>().ShouldBeOfType<FileWatchingBackend>().Backend.ShouldBeOfType<InMemoryBackend>();

        Should.Throw<ArgumentException>(() => new ServiceCollection().AddI18NextLocalization(i18n => i18n.WatchTranslationFiles("")));
    }

    private static async Task<TranslationsChangedEventArgs> WaitForChange(INotifyingTranslationBackend backend)
    {
        var completion = new TaskCompletionSource<TranslationsChangedEventArgs>();
        backend.TranslationsChanged += (_, e) => completion.TrySetResult(e);

        var finished = await Task.WhenAny(completion.Task, Task.Delay(Timeout));
        finished.ShouldBe(completion.Task);

        return await completion.Task;
    }
}
