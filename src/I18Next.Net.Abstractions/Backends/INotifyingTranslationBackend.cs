using System;

namespace I18Next.Net.Backends;

/// <summary>
///     A backend which notifies when the translations of a namespace have changed, e.g. because the translation file was
///     edited. Translators drop their cached namespaces and UI integrations render again.
/// </summary>
public interface INotifyingTranslationBackend : ITranslationBackend
{
    /// <summary>
    ///     Raised after translations have changed.
    /// </summary>
    event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;
}
