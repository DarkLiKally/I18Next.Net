using System;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Blazor.Tests;

internal sealed class NotifyingBackend(ITranslationBackend backend) : INotifyingTranslationBackend
{
    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    public int SubscriberCount => TranslationsChanged?.GetInvocationList().Length ?? 0;

    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        return backend.LoadNamespaceAsync(language, @namespace);
    }

    public void RaiseTranslationsChanged(string language, string @namespace)
    {
        TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(language, @namespace));
    }
}
