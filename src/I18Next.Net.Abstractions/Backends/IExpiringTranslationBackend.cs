using System;

namespace I18Next.Net.Backends;

/// <summary>
///     A backend whose loaded namespaces should be reloaded by the translator after a certain amount of time.
/// </summary>
public interface IExpiringTranslationBackend : ITranslationBackend
{
    /// <summary>
    ///     The time after which a loaded namespace is considered stale. <c>null</c> keeps loaded namespaces forever.
    /// </summary>
    TimeSpan? CacheExpiration { get; }
}
