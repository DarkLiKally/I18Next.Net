using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations through a delegate, similar to the i18next-resources-to-backend.
/// </summary>
public class FuncBackend : ITranslationBackend
{
    private readonly Func<string, string, Task<ITranslationTree>> _loader;

    public FuncBackend(Func<string, string, Task<ITranslationTree>> loader)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    public FuncBackend(Func<string, string, ITranslationTree> loader)
        : this(loader == null ? throw new ArgumentNullException(nameof(loader)) : (language, ns) => Task.FromResult(loader(language, ns)))
    {
    }

    /// <summary>
    ///     Loads the language part of a regional language like "de" for "de-DE" when the delegate returns nothing for the
    ///     regional language.
    /// </summary>
    public bool FallbackToLanguagePart { get; set; } = true;

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var tree = await _loader(language, @namespace).ConfigureAwait(false);

        if (tree != null || !FallbackToLanguagePart)
            return tree;

        var languagePart = BackendUtilities.GetLanguagePart(language);

        return languagePart == language ? null : await _loader(languagePart, @namespace).ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a backend from a delegate returning the JSON content of a namespace.
    /// </summary>
    public static FuncBackend FromJson(Func<string, string, Task<string>> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return new FuncBackend(async (language, ns) =>
        {
            var json = await loader(language, ns).ConfigureAwait(false);

            return json == null ? null : JsonTranslationReader.Read(json, CreateBuilder(treeBuilderFactory, ns));
        });
    }

    /// <summary>
    ///     Creates a backend from a delegate returning the JSON content of a namespace.
    /// </summary>
    public static FuncBackend FromJson(Func<string, string, string> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return FromJson((language, ns) => Task.FromResult(loader(language, ns)), treeBuilderFactory);
    }

    /// <summary>
    ///     Creates a backend from a delegate returning a stream with the JSON content of a namespace, e.g. an embedded resource.
    /// </summary>
    public static FuncBackend FromStream(Func<string, string, Task<Stream>> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return new FuncBackend(async (language, ns) =>
        {
            var stream = await loader(language, ns).ConfigureAwait(false);

            if (stream == null)
                return null;

            using (stream)
                return await JsonTranslationReader.ReadAsync(stream, CreateBuilder(treeBuilderFactory, ns)).ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     Creates a backend from a delegate returning a stream with the JSON content of a namespace, e.g. an embedded resource.
    /// </summary>
    public static FuncBackend FromStream(Func<string, string, Stream> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return FromStream((language, ns) => Task.FromResult(loader(language, ns)), treeBuilderFactory);
    }

    /// <summary>
    ///     Creates a backend from a delegate returning the resources of a namespace as nested dictionaries, lists or objects.
    /// </summary>
    public static FuncBackend FromObject(Func<string, string, Task<object>> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return new FuncBackend(async (language, ns) =>
        {
            var resources = await loader(language, ns).ConfigureAwait(false);

            if (resources == null)
                return null;

            var builder = CreateBuilder(treeBuilderFactory, ns);

            foreach (var entry in GetEntries(resources))
                AddValue(entry.Key, entry.Value, builder);

            return builder.Build();
        });
    }

    /// <summary>
    ///     Creates a backend from a delegate returning the resources of a namespace as nested dictionaries, lists or objects.
    /// </summary>
    public static FuncBackend FromObject(Func<string, string, object> loader, ITranslationTreeBuilderFactory treeBuilderFactory = null)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        return FromObject((language, ns) => Task.FromResult(loader(language, ns)), treeBuilderFactory);
    }

    private static ITranslationTreeBuilder CreateBuilder(ITranslationTreeBuilderFactory treeBuilderFactory, string @namespace)
    {
        var builder = (treeBuilderFactory ?? new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>()).Create();
        builder.Namespace = @namespace;

        return builder;
    }

    private static IEnumerable<KeyValuePair<string, object>> GetEntries(object value)
    {
        switch (value)
        {
            case IDictionary<string, object> dictionary:
                return dictionary;
            case IDictionary<string, string> dictionary:
                return EnumerateStringDictionary(dictionary);
            case IDictionary dictionary:
                return EnumerateDictionary(dictionary);
            case JsonElement { ValueKind: JsonValueKind.Object } element:
                return element.ToDictionary();
            default:
                return value.ToDictionary();
        }
    }

    private static IEnumerable<KeyValuePair<string, object>> EnumerateStringDictionary(IDictionary<string, string> dictionary)
    {
        foreach (var entry in dictionary)
            yield return new KeyValuePair<string, object>(entry.Key, entry.Value);
    }

    private static IEnumerable<KeyValuePair<string, object>> EnumerateDictionary(IDictionary dictionary)
    {
        foreach (DictionaryEntry entry in dictionary)
            yield return new KeyValuePair<string, object>(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), entry.Value);
    }

    private static void AddValue(string key, object value, ITranslationTreeBuilder builder)
    {
        switch (value)
        {
            case null:
                return;
            case string text:
                builder.AddTranslation(key, text);
                return;
            case bool flag:
                builder.AddTranslation(key, flag ? bool.TrueString : bool.FalseString);
                return;
            case JsonElement element:
                AddValue(key, element.ToObject(), builder);
                return;
            case IFormattable formattable:
                builder.AddTranslation(key, formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            case IDictionary or IDictionary<string, object> or IDictionary<string, string>:
                break;
            case IEnumerable items:
                var index = 0;

                foreach (var item in items)
                    AddValue($"{key}.{index++}", item, builder);

                return;
        }

        foreach (var entry in GetEntries(value))
            AddValue($"{key}.{entry.Key}", entry.Value, builder);
    }
}
