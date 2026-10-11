using System;

namespace I18Next.Net.Backends;

/// <summary>
///     Combines several backends; the first backend providing a namespace wins. Replaced by the
///     <see cref="ChainedBackend" />, which behaves the same.
/// </summary>
/// <param name="backends">The backends in order of precedence.</param>
[Obsolete("Use ChainedBackend instead.")]
public class CompositeBackend(params ITranslationBackend[] backends) : ChainedBackend(backends);
