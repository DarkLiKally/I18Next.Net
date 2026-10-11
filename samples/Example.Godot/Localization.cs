using Godot;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Godot;
using I18Next.Net.Plugins;

namespace Example.Godot;

public partial class Localization : Node
{
    private FileWatchingBackend _watchingBackend;

    public override void _EnterTree()
    {
        ITranslationBackend backend = new GodotFileBackend("res://locales");

        if (OS.HasFeature("editor"))
            backend = _watchingBackend = new FileWatchingBackend(backend, ProjectSettings.GlobalizePath("res://locales"));

        var logger = new TraceLogger();
        var translator = new DefaultTranslator(backend, logger, new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 },
            new DefaultInterpolator(logger));
        var i18n = new I18NextNet(backend, translator) { Language = "en" };
        i18n.SetFallbackLanguages("en");

        I18NextGodot.Register(i18n, "en", "de");
    }

    public override void _ExitTree()
    {
        I18NextGodot.Unregister();
        _watchingBackend?.Dispose();
    }
}
