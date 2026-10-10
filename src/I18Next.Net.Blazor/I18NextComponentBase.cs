using System;
using System.Threading.Tasks;

using I18Next.Net.Backends;

using Microsoft.AspNetCore.Components;

namespace I18Next.Net.Blazor;

/// <summary>
///     A component which translates into the language of the current user and renders again when the language or the
///     translations change.
/// </summary>
public abstract class I18NextComponentBase : ComponentBase, IDisposable
{
    private bool _subscribed;

    /// <summary>
    ///     The language of the current user.
    /// </summary>
    [Inject]
    protected IBlazorI18Next I18n { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_subscribed)
        {
            I18n.LanguageChanged += OnLanguageChanged;
            I18n.TranslationsChanged += OnTranslationsChanged;
            _subscribed = true;
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>
    ///     Translates the given key into the language of the current user.
    /// </summary>
    /// <param name="key">Key to be translated, optionally prefixed with a namespace like <c>common:save</c>.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    protected string T(string key, object args = null)
    {
        return I18n.T(key, args);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || !_subscribed)
            return;

        I18n.LanguageChanged -= OnLanguageChanged;
        I18n.TranslationsChanged -= OnTranslationsChanged;
        _subscribed = false;
    }

    private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
