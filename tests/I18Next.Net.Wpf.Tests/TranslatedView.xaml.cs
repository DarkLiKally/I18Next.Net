namespace I18Next.Net.Wpf.Tests;

public partial class TranslatedView
{
    public TranslatedView()
    {
        InitializeComponent();
    }

    public string Items => ItemsText.Text;

    public string Title => TitleText.Text;
}
