using System.Threading;

using I18Next.Net.Xaml;

namespace I18Next.Net.Maui;

/// <summary>
///     Holds the I18Next instance used by the <see cref="TExtension" /> markup extension.
/// </summary>
public static class I18NextXaml
{
    private static readonly object SyncRoot = new();

    private static II18Next _instance;
    private static TranslationSource _source;

    /// <summary>
    ///     The instance translating the XAML. <see cref="MauiAppBuilderExtensions.UseI18Next(Microsoft.Maui.Hosting.MauiAppBuilder)" />
    ///     sets it from the service provider. Setting another instance later translates all views again.
    /// </summary>
    public static II18Next Instance
    {
        get => _instance;
        set
        {
            lock (SyncRoot)
            {
                _instance = value;

                if (_source != null)
                    _source.I18Next = value;
            }
        }
    }

    internal static TranslationSource Source
    {
        get
        {
            lock (SyncRoot)
                return _source ??= new TranslationSource(SynchronizationContext.Current) { I18Next = _instance };
        }
    }
}
