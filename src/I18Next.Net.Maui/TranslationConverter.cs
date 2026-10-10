using System;
using System.Globalization;

using I18Next.Net.Xaml;

using Microsoft.Maui.Controls;

namespace I18Next.Net.Maui;

internal sealed class TranslationConverter(TranslationSource source, string key, string @namespace, object args, object count)
    : IValueConverter, IMultiValueConverter
{
    private readonly object _args = args;
    private readonly object _count = count;
    private readonly string _key = key;
    private readonly string _namespace = @namespace;
    private readonly TranslationSource _source = source;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return _source.Translate(_key, _namespace, _args, _count);
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var index = 1;
        var args = _args is BindingBase ? GetValue(values[index++]) : _args;
        var count = _count is BindingBase ? GetValue(values[index]) : _count;

        return _source.Translate(_key, _namespace, args, count);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static object GetValue(object value)
    {
        return value == BindableProperty.UnsetValue ? null : value;
    }
}
