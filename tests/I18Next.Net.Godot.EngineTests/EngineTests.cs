using System;
using System.Threading.Tasks;

using Godot;

using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net.Godot.EngineTests;

public partial class EngineTests : Node
{
    private const string HotReloadPath = "user://hot-reload";

    private int _checks;
    private TranslationChangedCounter _counter;
    private int _failures;
    private RichTextLabel _label;

    public override async void _Ready()
    {
        try
        {
            await RunAsync();
        }
        catch (Exception e)
        {
            _failures++;
            GD.PrintErr($"FAIL unexpected exception: {e}");
        }

        GD.Print($"{_checks - _failures} of {_checks} checks passed");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private static I18NextNet CreateI18Next(ITranslationBackend backend)
    {
        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var translator = new DefaultTranslator(backend, logger, pluralResolver, new DefaultInterpolator(logger));

        return new I18NextNet(backend, translator) { Language = "en" };
    }

    private async Task RunAsync()
    {
        _counter = new TranslationChangedCounter();
        AddChild(_counter);

        _label = new RichTextLabel { Text = "title", FitContent = true };
        AddChild(_label);

        var i18Next = CreateI18Next(new GodotFileBackend());
        i18Next.SetFallbackLanguages("en");

        TranslationServer.SetLocale("fr");
        I18NextGodot.Register(i18Next, "en", "de");

        Check("Register sets the locale", TranslationServer.GetLocale(), "en");

        TranslationServer.SetLocale("de");
        await NextFrameAsync();

        Check("Locale changed before the first frame changes language", i18Next.Language, "de");

        i18Next.Language = "en";
        await NextFrameAsync();

        Check("Tr", Tr("title"), "Hello");
        Check("TrN one", TrN("inbox", "inbox", 1), "One message");
        Check("TrN other", TrN("inbox", "inbox", 5), "5 messages");
        Check("Tr with context", Tr("friend", "male"), "A boyfriend");
        Check("Tr with unknown context", Tr("friend", "female"), "A friend");
        Check("Tr without translation", Tr("Score: 10"), "Score: 10");
        Check("TrN without translation", TrN("apple", "apples", 2), "apples");
        Check("T with arguments", this.T("greeting", new { name = "Godot" }), "Hello Godot");
        Check("Auto translated control", _label.GetParsedText(), "Hello");

        _counter.Count = 0;
        TranslationServer.SetLocale("de_DE");
        await NextFrameAsync();

        Check("Godot locale changes language", i18Next.Language, "de-DE");
        Check("Tr after locale change", Tr("title"), "Hallo");
        Check("TrN after locale change", TrN("inbox", "inbox", 3), "3 Nachrichten");
        Check("Tr falls back to fallback locale", Tr("onlyEnglish"), "Only English");
        Check("T after locale change", this.T("greeting", new { name = "Godot" }), "Hallo Godot");
        Check("Auto translated control after locale change", _label.GetParsedText(), "Hallo");
        Check("Nodes notified after locale change", _counter.Count > 0, true);
        Check("Nodes notified after language changed", _counter.Text, "Hallo");

        _counter.Count = 0;
        i18Next.Language = "en";
        await NextFrameAsync();

        Check("Language changes Godot locale", TranslationServer.GetLocale(), "en");
        Check("Auto translated control after language change", _label.GetParsedText(), "Hello");
        Check("Nodes notified once after language change", _counter.Count, 1);

        await Task.Run(() => i18Next.Language = "de");
        await NextFrameAsync();
        await NextFrameAsync();

        Check("Language changed on another thread changes Godot locale", TranslationServer.GetLocale(), "de");

        await RunHotReloadAsync();

        I18NextGodot.Unregister();
        await NextFrameAsync();

        Check("Unregister removes translations", Tr("title"), "title");
        Check("Unregister updates controls", _label.GetParsedText(), "title");
        Check("Unregister clears instance", I18NextGodot.Instance == null, true);
    }

    private async Task RunHotReloadAsync()
    {
        DirAccess.MakeDirRecursiveAbsolute($"{HotReloadPath}/de");
        WriteFile($"{HotReloadPath}/de/translation.json", """{ "title": "Vorher" }""");

        var directory = ProjectSettings.GlobalizePath(HotReloadPath);
        using var backend = new FileWatchingBackend(new GodotFileBackend(HotReloadPath), directory);
        var i18Next = CreateI18Next(backend);
        i18Next.Language = "de";

        I18NextGodot.Register(i18Next, "de");
        await NextFrameAsync();

        Check("Registering another instance keeps the locale", TranslationServer.GetLocale(), "de");
        Check("Registering another instance replaces translations", Tr("title"), "Vorher");
        Check("Registering another instance updates controls", _label.GetParsedText(), "Vorher");

        _counter.Count = 0;
        WriteFile($"{HotReloadPath}/de/translation.json", """{ "title": "Nachher" }""");

        for (var i = 0; i < 100 && _counter.Count == 0; i++)
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);

        await NextFrameAsync();

        Check("Hot reload notifies nodes", _counter.Count > 0, true);
        Check("Hot reload changes Tr", Tr("title"), "Nachher");
        Check("Hot reload updates controls", _label.GetParsedText(), "Nachher");
    }

    private static void WriteFile(string path, string content)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }

    private async Task NextFrameAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Check<T>(string name, T actual, T expected)
    {
        _checks++;

        if (Equals(actual, expected))
        {
            GD.Print($"PASS {name}");

            return;
        }

        _failures++;
        GD.PrintErr($"FAIL {name}: expected '{expected}' but was '{actual}'");
    }
}
