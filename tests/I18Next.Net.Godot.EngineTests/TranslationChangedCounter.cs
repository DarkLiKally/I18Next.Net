using Godot;

namespace I18Next.Net.Godot.EngineTests;

public partial class TranslationChangedCounter : Node
{
    public int Count { get; set; }

    public string Text { get; set; }

    public override void _Notification(int what)
    {
        if (what != NotificationTranslationChanged)
            return;

        Count++;
        Text = this.T("title");
    }
}
