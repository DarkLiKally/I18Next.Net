using System;

namespace I18Next.Net.Backends;

[Obsolete("Use ChainedBackend instead.")]
public class CompositeBackend(params ITranslationBackend[] backends) : ChainedBackend(backends);
