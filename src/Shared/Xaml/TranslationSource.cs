using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

using I18Next.Net.Backends;
using I18Next.Net.Internal;

namespace I18Next.Net.Xaml;

/// <summary>
///     Translates for XAML bindings and notifies them when the language or the translations have changed. Notifications
///     are raised on the thread of the given synchronization context.
/// </summary>
internal sealed class TranslationSource(SynchronizationContext synchronizationContext) : INotifyPropertyChanged
{
    private readonly SynchronizationContext _synchronizationContext = synchronizationContext;
    private readonly int _threadId = Environment.CurrentManagedThreadId;

    private INotifyingTranslationBackend _backend;
    private int _refreshPending;

    public II18Next I18Next
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            if (field != null)
                field.LanguageChanged -= OnLanguageChanged;

            if (_backend != null)
                _backend.TranslationsChanged -= OnTranslationsChanged;

            field = value;
            _backend = value?.Backend as INotifyingTranslationBackend;

            if (value != null)
                value.LanguageChanged += OnLanguageChanged;

            if (_backend != null)
                _backend.TranslationsChanged += OnTranslationsChanged;

            Refresh();
        }
    }

    public string Language => I18Next?.Language;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Translate(string key, string @namespace = null, object args = null, object count = null)
    {
        var i18Next = I18Next;

        if (i18Next == null || string.IsNullOrEmpty(key))
            return key;

        var arguments = CreateArguments(args, count);

        return @namespace == null ? i18Next.T(key, arguments) : i18Next.T(i18Next.Language, @namespace, key, arguments);
    }

    private static object CreateArguments(object args, object count)
    {
        if (args is IConvertible or IFormattable or (IEnumerable and not IDictionary<string, object>))
            args = new Dictionary<string, object> { ["value"] = args };

        if (count == null)
            return args;

        return new Dictionary<string, object>(ObjectExtensions.ObjectToDictionary(args)) { ["count"] = ParseCount(count) };
    }

    private static object ParseCount(object count)
    {
        if (count is not string text)
            return count;

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            return integer;

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : count;
    }

    private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
    {
        Refresh();
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_synchronizationContext == null || Environment.CurrentManagedThreadId == _threadId)
            OnPropertyChanged();
        else if (Interlocked.Exchange(ref _refreshPending, 1) == 0)
            _synchronizationContext.Post(_ => OnPropertyChanged(), null);
    }

    private void OnPropertyChanged()
    {
        Interlocked.Exchange(ref _refreshPending, 0);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
