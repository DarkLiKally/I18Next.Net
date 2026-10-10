using System.ComponentModel;
using System.Runtime.CompilerServices;

using I18Next.Net;

namespace Example.Wpf;

public class MainViewModel(II18Next i18Next) : INotifyPropertyChanged
{
    private readonly II18Next _i18Next = i18Next;

    public int Count
    {
        get;
        set => SetField(ref field, value);
    } = 3;

    public string Language
    {
        get => _i18Next.Language;
        set
        {
            _i18Next.Language = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = "Jane";

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        field = value;
        OnPropertyChanged(propertyName);
    }
}
