using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class MachineTranslationMissingKeyHandlerFixture
{
    private readonly InMemoryBackend _files = new();
    private readonly InMemoryBackend _memory = new();
    private readonly List<(string Text, string Source, string Target)> _requests = [];
    private readonly FuncMachineTranslator _translator;

    public MachineTranslationMissingKeyHandlerFixture()
    {
        _files.AddTranslations("en", "translation", new Dictionary<string, string>
        {
            ["greeting"] = "Hello {{name}}",
            ["item_one"] = "{{count}} item",
            ["item_other"] = "{{count}} items",
            ["friend"] = "A friend",
            ["empty"] = ""
        });
        _files.AddTranslation("de", "translation", "existing", "Vorhanden");

        _translator = new FuncMachineTranslator((texts, source, target, _) =>
        {
            lock (_requests)
                _requests.AddRange(texts.Select(t => (t, source, target)));

            return Task.FromResult<IReadOnlyList<string>>(texts.Select(t => $"[{target}] {t}").ToList());
        });
    }

    [Fact]
    public void MissingKey_ShouldBeTranslatedAndUsedOnTheNextCall()
    {
        var i18Next = CreateI18Next(new MachineTranslationMissingKeyHandler(_translator, _files, "en", _memory) { WaitForTranslation = true });

        i18Next.T("greeting", new { name = "Ana" }).ShouldBe("Hello Ana");
        i18Next.T("greeting", new { name = "Ana" }).ShouldBe("[de] Hello Ana");
        i18Next.T("existing").ShouldBe("Vorhanden");

        _requests.ShouldBe([("Hello {{name}}", "en", "de")]);
    }

    [Fact]
    public async Task MissingKey_WithoutWaiting_ShouldBeTranslatedInTheBackground()
    {
        var translated = new TaskCompletionSource<MachineTranslatedKey>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new MachineTranslationMissingKeyHandler(_translator, _files, "en", key =>
        {
            translated.SetResult(key);

            return Task.CompletedTask;
        });
        var i18Next = CreateI18Next(handler);

        i18Next.T("greeting", new { name = "Ana" }).ShouldBe("Hello Ana");

        var key = await translated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        key.Language.ShouldBe("de");
        key.Namespace.ShouldBe("translation");
        key.Key.ShouldBe("greeting");
        key.SourceLanguage.ShouldBe("en");
        key.SourceText.ShouldBe("Hello {{name}}");
        key.Text.ShouldBe("[de] Hello {{name}}");
    }

    [Fact]
    public async Task ConcurrentRequestsForTheSameKey_ShouldBeTranslatedOnce()
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var translator = new FuncMachineTranslator(async (texts, _, _, _) =>
        {
            Interlocked.Increment(ref calls);
            await release.Task;

            return texts;
        });
        var handler = new MachineTranslationMissingKeyHandler(translator, _files, "en", _memory) { WaitForTranslation = true };
        var args = new MissingKeyEventArgs("de", "translation", "greeting", ["greeting"]);

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() => handler.HandleMissingKeyAsync(this, args))).ToList();
        release.SetResult(true);
        await Task.WhenAll(tasks);
        await handler.HandleMissingKeyAsync(this, args);

        calls.ShouldBe(1);
    }

    [Fact]
    public async Task PluralKey_ShouldUseTheMatchingOrOtherSourceForm()
    {
        var stored = new ConcurrentBag<MachineTranslatedKey>();
        var handler = CreateCollectingHandler(stored);

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("ru", "translation", "item", ["item", "item_one"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("ru", "translation", "item", ["item", "item_few"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("ru", "translation", "friend", ["friend", "friend_male"]));

        stored.OrderBy(k => k.Key).Select(k => (k.Key, k.SourceText)).ShouldBe([
            ("friend", "A friend"),
            ("item_few", "{{count}} items"),
            ("item_one", "{{count}} item")
        ]);
    }

    [Fact]
    public async Task SourceLanguageOrMissingSource_ShouldBeIgnored()
    {
        var stored = new ConcurrentBag<MachineTranslatedKey>();
        var handler = CreateCollectingHandler(stored);

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("en", "translation", "greeting", ["greeting"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("EN-us", "translation", "greeting", ["greeting"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("cimode", "translation", "greeting", ["greeting"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("dev", "translation", "greeting", ["greeting"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "unknown", ["unknown"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "empty", []));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "other", "greeting", null));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "item", ["item"]));

        stored.ShouldBeEmpty();
        _requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailingTranslation_ShouldBeLoggedAndNotThrown()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var translator = new FuncMachineTranslator((_, _, _, _) => throw new MachineTranslationException("Quota exceeded"));
        var handler = new MachineTranslationMissingKeyHandler(translator, _files, "en", _memory) { WaitForTranslation = true, Logger = logger };

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "greeting", ["greeting"]));

        logger.Received(1).Log(LogLevel.Warning, Arg.Is<Exception>(e => e.Message == "Quota exceeded"), Arg.Any<string>(), Arg.Any<object[]>());
        _memory.HasNamespace("de", "translation").ShouldBeFalse();
    }

    [Fact]
    public async Task InMemoryTarget_ShouldBeFilledWithTheExistingNamespaceFirst()
    {
        var handler = new MachineTranslationMissingKeyHandler(_translator, _files, "en", _memory) { WaitForTranslation = true };

        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "greeting", ["greeting"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("de", "translation", "friend", ["friend"]));
        await handler.HandleMissingKeyAsync(this, new MissingKeyEventArgs("fr", "translation", "friend", ["friend"]));

        var de = await _memory.LoadNamespaceAsync("de", "translation");
        de.GetAllValues().ShouldBe(new Dictionary<string, string>
        {
            ["existing"] = "Vorhanden",
            ["greeting"] = "[de] Hello {{name}}",
            ["friend"] = "[de] A friend"
        }, ignoreOrder: true);
        (await _memory.LoadNamespaceAsync("fr", "translation")).GetAllValues().Count.ShouldBe(1);
    }

    [Fact]
    public void InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new MachineTranslationMissingKeyHandler(null, _files, "en", _memory));
        Should.Throw<ArgumentNullException>(() => new MachineTranslationMissingKeyHandler(_translator, null, "en", _memory));
        Should.Throw<ArgumentNullException>(() => new MachineTranslationMissingKeyHandler(_translator, _files, "en", (InMemoryBackend)null));
        Should.Throw<ArgumentNullException>(() => new MachineTranslationMissingKeyHandler(_translator, _files, "en", (Func<MachineTranslatedKey, Task>)null));
        Should.Throw<ArgumentException>(() => new MachineTranslationMissingKeyHandler(_translator, _files, " ", _memory));
        Should.Throw<ArgumentNullException>(() => new MachineTranslationMissingKeyHandler(_translator, _files, "en", _memory).HandleMissingKeyAsync(this, null));
    }

    private MachineTranslationMissingKeyHandler CreateCollectingHandler(ConcurrentBag<MachineTranslatedKey> stored)
    {
        return new MachineTranslationMissingKeyHandler(_translator, _files, "en", key =>
        {
            stored.Add(key);

            return Task.CompletedTask;
        }) { WaitForTranslation = true };
    }

    private I18NextNet CreateI18Next(IMissingKeyHandler handler)
    {
        var backend = new ChainedBackend(_memory, _files);
        var translator = new DefaultTranslator(backend);
        translator.MissingKeyHandlers.Add(handler);

        var i18Next = new I18NextNet(backend, translator) { Language = "de" };
        i18Next.SetFallbackLanguages("en");

        return i18Next;
    }
}
