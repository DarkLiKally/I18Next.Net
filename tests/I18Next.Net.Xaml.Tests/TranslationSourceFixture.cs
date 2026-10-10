using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Xaml.Tests;

public class TranslationSourceFixture
{
    private static I18NextNet CreateI18Next(ITranslationBackend backend = null)
    {
        if (backend == null)
        {
            var inMemoryBackend = new InMemoryBackend();
            inMemoryBackend.AddTranslation("en", "translation", "menu.title", "Menu");
            inMemoryBackend.AddTranslation("en", "translation", "greeting", "Hello {{Name}}");
            inMemoryBackend.AddTranslation("en", "translation", "total", "Total: {{value}}");
            inMemoryBackend.AddTranslation("en", "translation", "guests", "Guests: {{value, list}}");
            inMemoryBackend.AddTranslation("en", "translation", "item_one", "{{count}} item of {{Name}}");
            inMemoryBackend.AddTranslation("en", "translation", "item_other", "{{count}} items of {{Name}}");
            inMemoryBackend.AddTranslation("en", "common", "menu.title", "Common menu");
            inMemoryBackend.AddTranslation("de", "translation", "menu.title", "Menü");
            backend = inMemoryBackend;
        }

        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));

        return new I18NextNet(backend, translator) { Language = "en" };
    }

    private static List<string> RecordChanges(INotifyPropertyChanged source)
    {
        var changes = new List<string>();
        source.PropertyChanged += (_, e) =>
        {
            lock (changes)
                changes.Add(e.PropertyName);
        };

        return changes;
    }

    private static void RunOnOtherThread(Action action)
    {
        var thread = new Thread(() => action());
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void Translate_Key_ShouldTranslateInCurrentLanguage()
    {
        var i18Next = CreateI18Next();
        var source = new TranslationSource(null) { I18Next = i18Next };

        source.Translate("menu.title").ShouldBe("Menu");
        source.Language.ShouldBe("en");

        i18Next.Language = "de";

        source.Translate("menu.title").ShouldBe("Menü");
        source.Language.ShouldBe("de");
    }

    [Fact]
    public void Translate_Namespace_ShouldTranslateFromNamespace()
    {
        var source = new TranslationSource(null) { I18Next = CreateI18Next() };

        source.Translate("menu.title", "common").ShouldBe("Common menu");
    }

    [Fact]
    public void Translate_ObjectArgs_ShouldInterpolateProperties()
    {
        var source = new TranslationSource(null) { I18Next = CreateI18Next() };

        source.Translate("greeting", args: new { Name = "Jane" }).ShouldBe("Hello Jane");
        source.Translate("greeting", args: new Dictionary<string, object> { ["Name"] = "John" }).ShouldBe("Hello John");
    }

    [Fact]
    public void Translate_ValueArgs_ShouldBeAvailableAsValue()
    {
        var source = new TranslationSource(null) { I18Next = CreateI18Next() };

        source.Translate("total", args: "many").ShouldBe("Total: many");
        source.Translate("total", args: 42).ShouldBe("Total: 42");
        source.Translate("total", args: TimeSpan.FromMinutes(1)).ShouldBe("Total: 00:01:00");
        source.Translate("guests", args: new[] { "Anna", "Ben" }).ShouldBe("Guests: Anna and Ben");
    }

    [Theory]
    [InlineData(1, "1 item of Jane")]
    [InlineData(3, "3 items of Jane")]
    [InlineData("1", "1 item of Jane")]
    [InlineData("3", "3 items of Jane")]
    [InlineData("1.5", "1.5 items of Jane")]
    public void Translate_Count_ShouldResolvePlural(object count, string expected)
    {
        var source = new TranslationSource(null) { I18Next = CreateI18Next() };

        source.Translate("item", args: new { Name = "Jane" }, count: count).ShouldBe(expected);
    }

    [Fact]
    public void Translate_CountWithDictionaryArgs_ShouldNotChangeArgs()
    {
        var source = new TranslationSource(null) { I18Next = CreateI18Next() };
        var args = new Dictionary<string, object> { ["Name"] = "Jane" };

        source.Translate("item", args: args, count: 2).ShouldBe("2 items of Jane");

        args.ShouldNotContainKey("count");
    }

    [Fact]
    public void Translate_InvalidCount_ShouldPassCountAsIs()
    {
        var i18Next = Substitute.For<II18Next>();
        var source = new TranslationSource(null) { I18Next = i18Next };

        source.Translate("item", count: "many");

        i18Next.Received().T("item", Arg.Is<object>(a => (string)((IDictionary<string, object>)a)["count"] == "many"));
    }

    [Fact]
    public void Translate_WithoutInstanceOrKey_ShouldReturnKey()
    {
        new TranslationSource(null).Translate("menu.title").ShouldBe("menu.title");
        new TranslationSource(null).Language.ShouldBeNull();
        new TranslationSource(null) { I18Next = CreateI18Next() }.Translate("").ShouldBe("");
        new TranslationSource(null) { I18Next = CreateI18Next() }.Translate(null).ShouldBeNull();
    }

    [Fact]
    public void LanguageChanged_ShouldNotifyAllProperties()
    {
        var i18Next = CreateI18Next();
        var source = new TranslationSource(null) { I18Next = i18Next };
        var changes = RecordChanges(source);

        i18Next.Language = "de";

        changes.ShouldBe([string.Empty]);
    }

    [Fact]
    public void TranslationsChanged_ShouldNotify()
    {
        var backend = Substitute.For<INotifyingTranslationBackend>();
        var i18Next = Substitute.For<II18Next>();
        i18Next.Backend.Returns(backend);

        var source = new TranslationSource(null) { I18Next = i18Next };
        var changes = RecordChanges(source);

        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs("en", "translation"));

        changes.ShouldBe([string.Empty]);
    }

    [Fact]
    public void SetI18Next_ShouldNotifyAndUnsubscribeFromPreviousInstance()
    {
        var backend = Substitute.For<INotifyingTranslationBackend>();
        var previous = Substitute.For<II18Next>();
        previous.Backend.Returns(backend);
        var next = CreateI18Next();

        var source = new TranslationSource(null) { I18Next = previous };
        var changes = RecordChanges(source);

        source.I18Next = previous;
        changes.ShouldBeEmpty();

        source.I18Next = next;
        changes.Count.ShouldBe(1);

        previous.LanguageChanged += Raise.EventWith(previous, new LanguageChangedEventArgs("en", "de"));
        backend.TranslationsChanged += Raise.EventWith(backend, new TranslationsChangedEventArgs(null, null));
        changes.Count.ShouldBe(1);

        source.I18Next = null;
        changes.Count.ShouldBe(2);

        next.Language = "de";
        changes.Count.ShouldBe(2);
        source.Translate("menu.title").ShouldBe("menu.title");
    }

    [Fact]
    public void ChangeOnOtherThread_ShouldPostOnceToSynchronizationContext()
    {
        var context = new QueueSynchronizationContext();
        var i18Next = CreateI18Next();
        var source = new TranslationSource(context) { I18Next = i18Next };
        var changes = RecordChanges(source);

        RunOnOtherThread(() =>
        {
            i18Next.Language = "de";
            i18Next.Language = "en";
        });

        changes.ShouldBeEmpty();
        context.Count.ShouldBe(1);

        context.RunAll();

        changes.ShouldBe([string.Empty]);

        RunOnOtherThread(() => i18Next.Language = "de");
        context.Count.ShouldBe(1);
    }

    [Fact]
    public void ChangeOnCreatingThread_ShouldNotifyImmediately()
    {
        var context = new QueueSynchronizationContext();
        var i18Next = CreateI18Next();
        var source = new TranslationSource(context) { I18Next = i18Next };
        var changes = RecordChanges(source);

        i18Next.Language = "de";

        changes.ShouldBe([string.Empty]);
        context.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ChangedTranslationFile_ShouldNotifyAndTranslateAgain()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(directory, "en"));
        File.WriteAllText(Path.Combine(directory, "en", "translation.json"), """{ "title": "Hello" }""");

        try
        {
            using var backend = new FileWatchingBackend(new JsonFileBackend(directory), directory) { Delay = TimeSpan.FromMilliseconds(50) };
            var source = new TranslationSource(null) { I18Next = CreateI18Next(backend) };
            var changed = new TaskCompletionSource<bool>();
            source.PropertyChanged += (_, _) => changed.TrySetResult(true);

            source.Translate("title").ShouldBe("Hello");

            File.WriteAllText(Path.Combine(directory, "en", "translation.json"), """{ "title": "Hi" }""");

            (await Task.WhenAny(changed.Task, Task.Delay(TimeSpan.FromSeconds(10)))).ShouldBe(changed.Task);
            source.Translate("title").ShouldBe("Hi");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class QueueSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object State)> _queue = new();

        public int Count
        {
            get
            {
                lock (_queue)
                    return _queue.Count;
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (_queue)
                _queue.Enqueue((d, state));
        }

        public void RunAll()
        {
            while (true)
            {
                (SendOrPostCallback Callback, object State) item;

                lock (_queue)
                {
                    if (_queue.Count == 0)
                        return;

                    item = _queue.Dequeue();
                }

                item.Callback(item.State);
            }
        }
    }
}
