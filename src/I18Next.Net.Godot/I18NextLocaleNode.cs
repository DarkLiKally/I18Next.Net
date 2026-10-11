using Godot;

namespace I18Next.Net.Godot;

/// <summary>
///     Changes the language of the registered instance when the Godot locale changes.
/// </summary>
internal partial class I18NextLocaleNode : Node
{
    public override void _Ready()
    {
        if (I18NextGodot.SyncLanguage())
            I18NextGodot.NotifyTranslationChanged();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            I18NextGodot.SyncLanguage();
    }
}
