using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace I18Next.Net.Maui.Tests;

public sealed record User(string Name);

public sealed class ViewModel : INotifyPropertyChanged
{
    public int Count
    {
        get;
        set => SetField(ref field, value);
    }

    public decimal Total
    {
        get;
        set => SetField(ref field, value);
    }

    public User User
    {
        get;
        set => SetField(ref field, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
