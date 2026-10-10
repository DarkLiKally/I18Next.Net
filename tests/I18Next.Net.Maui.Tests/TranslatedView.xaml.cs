using Microsoft.Maui.Controls;

namespace I18Next.Net.Maui.Tests;

public partial class TranslatedView : ContentView
{
    public TranslatedView()
    {
        InitializeComponent();
    }

    public string ItemsText => ItemsLabel.Text;

    public string TitleText => TitleLabel.Text;
}
