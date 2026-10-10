using I18Next.Net.Wpf;

namespace Example.Wpf;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel(I18NextXaml.Instance);
    }
}
