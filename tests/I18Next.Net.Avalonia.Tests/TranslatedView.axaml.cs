using Avalonia.Controls;

namespace I18Next.Net.Avalonia.Tests;

public partial class TranslatedView : UserControl
{
    public TranslatedView()
    {
        InitializeComponent();
    }

    public string Items => ItemsText.Text;

    public string Title => TitleText.Text;
}
