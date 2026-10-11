using Godot;

using I18Next.Net.Godot;

namespace Example.Godot;

public partial class Main : Control
{
    private int _count;
    private Label _inbox;

    public override void _Ready()
    {
        _inbox = GetNode<Label>("Layout/Inbox");
        GetNode<Button>("Layout/AddMessage").Pressed += OnAddMessagePressed;
        GetNode<Button>("Layout/SwitchLanguage").Pressed += OnSwitchLanguagePressed;

        UpdateInbox();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _inbox != null)
            UpdateInbox();
    }

    private void OnAddMessagePressed()
    {
        _count++;
        UpdateInbox();
    }

    private static void OnSwitchLanguagePressed()
    {
        TranslationServer.SetLocale(TranslationServer.GetLocale() == "de" ? "en" : "de");
    }

    private void UpdateInbox()
    {
        _inbox.Text = this.T("inbox", new { count = _count });
    }
}
