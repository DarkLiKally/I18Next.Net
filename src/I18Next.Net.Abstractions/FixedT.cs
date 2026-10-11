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

    /// <summary>
    ///     Creates a translator with a fixed language, namespace and key prefix.
    /// </summary>
    /// <param name="i18Next">The instance used to translate.</param>
    /// <param name="language">The language to translate into or <c>null</c> to use the current language.</param>
    /// <param name="namespace">
    ///     The namespace used for keys without namespace prefix or <c>null</c> to use the default namespace.
    /// </param>
    /// <param name="keyPrefix">The prefix put in front of each key, separated by a dot, or <c>null</c>.</param>
    /// <param name="namespaceSeparator">
    ///     The separator between namespace and key, which has to match the namespace separator of the translator.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="i18Next" /> is <c>null</c>.</exception>
    public FixedT(II18Next i18Next, string language = null, string @namespace = null, string keyPrefix = null, string namespaceSeparator = ":")
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Language = language;
        Namespace = @namespace;
        KeyPrefix = keyPrefix;
        NamespaceSeparator = namespaceSeparator;
    }

    /// <summary>
    ///     The prefix put in front of each key, separated by a dot, like the i18next <c>keyPrefix</c> option. <c>null</c>
    ///     or empty uses the keys as they are.
    /// </summary>
    public string KeyPrefix { get; }

    /// <summary>
    ///     The language to translate into or <c>null</c> to use the current language of the instance.
    /// </summary>
    public string Language { get; }

    /// <summary>
    ///     The namespace used for keys without namespace prefix or <c>null</c> to use the default namespace of the
    ///     instance. A namespace prefix in a key (e.g. <c>common:save</c>) takes precedence.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     The separator between namespace and key, <c>:</c> by default. It has to match the namespace separator of the
    ///     translator.
    /// </summary>
    public string NamespaceSeparator { get; }

    /// <summary>
    ///     Translates the given key with the fixed language, namespace and key prefix.
    /// </summary>
    /// <param name="key">Key to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    /// <seealso cref="II18Next.T(string, object)" />
    public string T(string key, object args = null)
    {
        return Language == null ? _i18Next.T(GetKey(key), args) : _i18Next.T(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Translates the given key with the fixed language, namespace and key prefix.
    /// </summary>
    /// <param name="key">Key to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    /// <seealso cref="II18Next.Ta(string, object)" />
    public Task<string> Ta(string key, object args = null)
    {
        return Language == null ? _i18Next.Ta(GetKey(key), args) : _i18Next.Ta(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Translates all values below the given key with the fixed language, namespace and key prefix. Nested groups are
    ///     returned as dictionaries, arrays as object arrays.
    /// </summary>
    /// <param name="key">Key of the group to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    /// <seealso cref="II18Next.TObject(string, object)" />
    public IDictionary<string, object> TObject(string key, object args = null)
    {
        return Language == null ? _i18Next.TObject(GetKey(key), args) : _i18Next.TObject(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Translates all values below the given key with the fixed language, namespace and key prefix. Nested groups are
    ///     returned as dictionaries, arrays as object arrays.
    /// </summary>
    /// <param name="key">Key of the group to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    /// <seealso cref="II18Next.TaObject(string, object)" />
    public Task<IDictionary<string, object>> TaObject(string key, object args = null)
    {
        return Language == null ? _i18Next.TaObject(GetKey(key), args) : _i18Next.TaObject(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Translates all values below the given key with the fixed language, namespace and key prefix and maps them to
    ///     the given type.
    /// </summary>
    /// <param name="key">Key of the group to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    /// <seealso cref="II18Next.T{TModel}(string, object)" />
    public TModel T<TModel>(string key, object args = null)
    {
        return Language == null ? _i18Next.T<TModel>(GetKey(key), args) : _i18Next.T<TModel>(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Translates all values below the given key with the fixed language, namespace and key prefix and maps them to
    ///     the given type.
    /// </summary>
    /// <param name="key">Key of the group to be translated, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    /// <seealso cref="II18Next.Ta{TModel}(string, object)" />
    public Task<TModel> Ta<TModel>(string key, object args = null)
    {
        return Language == null ? _i18Next.Ta<TModel>(GetKey(key), args) : _i18Next.Ta<TModel>(Language, GetKey(key), args);
    }

    /// <summary>
    ///     Checks whether a translation exists for the given key with the fixed language, namespace and key prefix.
    /// </summary>
    /// <param name="key">Key to be checked, optionally with a namespace prefix.</param>
    /// <param name="args">Additional arguments used to resolve the key, e.g. count or context.</param>
    /// <returns>Whether a translation exists.</returns>
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
