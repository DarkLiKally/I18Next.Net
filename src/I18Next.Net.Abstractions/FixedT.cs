using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net;

/// <summary>
///     Translates with a fixed language, namespace and key prefix, the equivalent of the i18next <c>getFixedT</c>.
/// </summary>
public sealed class FixedT
{
    private readonly II18Next _i18Next;

    public FixedT(II18Next i18Next, string language = null, string @namespace = null, string keyPrefix = null, string namespaceSeparator = ":")
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Language = language;
        Namespace = @namespace;
        KeyPrefix = keyPrefix;
        NamespaceSeparator = namespaceSeparator;
    }

    public string KeyPrefix { get; }

    public string Language { get; }

    public string Namespace { get; }

    public string NamespaceSeparator { get; }

    public string T(string key, object args = null)
    {
        return Language == null ? _i18Next.T(GetKey(key), args) : _i18Next.T(Language, GetKey(key), args);
    }

    public Task<string> Ta(string key, object args = null)
    {
        return Language == null ? _i18Next.Ta(GetKey(key), args) : _i18Next.Ta(Language, GetKey(key), args);
    }

    public IDictionary<string, object> TObject(string key, object args = null)
    {
        return Language == null ? _i18Next.TObject(GetKey(key), args) : _i18Next.TObject(Language, GetKey(key), args);
    }

    public Task<IDictionary<string, object>> TaObject(string key, object args = null)
    {
        return Language == null ? _i18Next.TaObject(GetKey(key), args) : _i18Next.TaObject(Language, GetKey(key), args);
    }

    public TModel T<TModel>(string key, object args = null)
    {
        return Language == null ? _i18Next.T<TModel>(GetKey(key), args) : _i18Next.T<TModel>(Language, GetKey(key), args);
    }

    public Task<TModel> Ta<TModel>(string key, object args = null)
    {
        return Language == null ? _i18Next.Ta<TModel>(GetKey(key), args) : _i18Next.Ta<TModel>(Language, GetKey(key), args);
    }

    public bool Exists(string key, object args = null)
    {
        return Language == null
            ? _i18Next.Exists(GetKey(key), args)
            : _i18Next.ExistsAsync(Language, GetKey(key), args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private string GetKey(string key)
    {
        string @namespace = null;
        var separatorIndex = string.IsNullOrEmpty(NamespaceSeparator) ? -1 : key.IndexOf(NamespaceSeparator, StringComparison.Ordinal);

        if (separatorIndex > 0)
        {
            @namespace = key.Substring(0, separatorIndex);
            key = key.Substring(separatorIndex + NamespaceSeparator.Length);
        }

        if (!string.IsNullOrEmpty(KeyPrefix))
            key = KeyPrefix + "." + key;

        @namespace ??= Namespace;

        return @namespace == null ? key : @namespace + NamespaceSeparator + key;
    }
}
