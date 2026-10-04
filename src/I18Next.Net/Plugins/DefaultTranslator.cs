using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.Logging;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Plugins;

public class DefaultTranslator : ITranslator
{
    private readonly ITranslationBackend _backend;
    private readonly IInterpolator _interpolator;
    private readonly ILogger _logger;
    private readonly IPluralResolver _pluralResolver;

    private readonly ConcurrentDictionary<(string Language, string Namespace), CachedTree> _treeCache = new();

    public DefaultTranslator(ITranslationBackend backend, ILogger logger, IPluralResolver pluralResolver, IInterpolator interpolator)
    {
        _backend = backend;
        _logger = logger;
        _pluralResolver = pluralResolver;
        _interpolator = interpolator;
    }

    public DefaultTranslator(ITranslationBackend backend)
    {
        _backend = backend;
        _logger = new TraceLogger();
        _pluralResolver = new DefaultPluralResolver();
        _interpolator = new DefaultInterpolator(_logger);
    }

    public DefaultTranslator(ITranslationBackend backend, IInterpolator interpolator)
    {
        _backend = backend;
        _logger = new TraceLogger();
        _pluralResolver = new DefaultPluralResolver();
        _interpolator = interpolator;
    }

    public bool AllowInterpolation { get; set; } = true;

    public bool AllowNesting { get; set; } = true;

    public bool AllowPostprocessing { get; set; } = true;

    public string ContextSeparator { get; set; } = "_";

    public string NamespaceSeparator { get; set; } = ":";

    public List<IMissingKeyHandler> MissingKeyHandlers { get; } = [];

    public event EventHandler<MissingKeyEventArgs> MissingKey;

    public List<IPostProcessor> PostProcessors { get; } = [];

    public virtual async Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        ValidateArguments(language, key, options);

        var actualNamespace = SplitNamespace(ref key, options);

        if (string.Equals(language, "cimode", StringComparison.OrdinalIgnoreCase))
            return $"{actualNamespace}{NamespaceSeparator}{key}";

        var result = await ResolveTranslationAsync(language, actualNamespace, key, args, options, true).ConfigureAwait(false)
                     ?? GetDefaultValue(language, args);

        if (result != null)
            return await ExtendTranslationAsync(result, key, language, args, options).ConfigureAwait(false);

        var groupValues = await ResolveGroupValuesAsync(language, actualNamespace, key, options).ConfigureAwait(false);

        if (groupValues == null)
            return key;

        if (args != null && args.TryGetValue("joinArrays", out var joinArrays) && joinArrays is string separator &&
            groupValues.Keys.All(k => k.IndexOf('.') < 0) && IsArray(groupValues))
        {
            var items = new List<string>(groupValues.Count);

            for (var i = 0; i < groupValues.Count; i++)
                items.Add(await ExtendTranslationAsync(groupValues[i.ToString(CultureInfo.InvariantCulture)], $"{key}.{i}", language, args, options)
                    .ConfigureAwait(false));

            return string.Join(separator, items);
        }

        return $"key '{actualNamespace}{NamespaceSeparator}{key} ({language})' returned an object instead of string.";
    }

    public virtual async Task<IDictionary<string, object>> TranslateObjectAsync(string language, string key, IDictionary<string, object> args,
        TranslationOptions options)
    {
        ValidateArguments(language, key, options);

        var actualNamespace = SplitNamespace(ref key, options);
        var groupValues = await ResolveGroupValuesAsync(language, actualNamespace, key, options).ConfigureAwait(false);

        if (groupValues == null)
        {
            await OnMissingKey(language, actualNamespace, key, [key]).ConfigureAwait(false);

            return null;
        }

        var result = new Dictionary<string, object>();

        foreach (var entry in groupValues)
        {
            var value = await ExtendTranslationAsync(entry.Value, $"{key}.{entry.Key}", language, args, options).ConfigureAwait(false);

            AddNestedValue(result, entry.Key.Split('.'), value);
        }

        foreach (var entry in result.ToList())
            result[entry.Key] = ConvertArrays(entry.Value);

        return result;
    }

    public virtual async Task<bool> ExistsAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        ValidateArguments(language, key, options);

        var actualNamespace = SplitNamespace(ref key, options);

        return await ResolveTranslationAsync(language, actualNamespace, key, args, options, false).ConfigureAwait(false) != null;
    }

    public void ClearCache()
    {
        _treeCache.Clear();
    }

    public void ClearCache(string language, string @namespace)
    {
        _treeCache.TryRemove((language, @namespace), out _);
    }

    private static void AddNestedValue(IDictionary<string, object> target, string[] path, string value)
    {
        for (var i = 0; i < path.Length - 1; i++)
        {
            if (!target.TryGetValue(path[i], out var child) || child is not IDictionary<string, object> childDictionary)
            {
                childDictionary = new Dictionary<string, object>();
                target[path[i]] = childDictionary;
            }

            target = childDictionary;
        }

        target[path[path.Length - 1]] = value;
    }

    private static object ConvertArrays(object value)
    {
        if (value is not IDictionary<string, object> dictionary)
            return value;

        foreach (var entry in dictionary.ToList())
            dictionary[entry.Key] = ConvertArrays(entry.Value);

        if (!IsArray(dictionary))
            return dictionary;

        var array = new object[dictionary.Count];

        for (var i = 0; i < array.Length; i++)
            array[i] = dictionary[i.ToString(CultureInfo.InvariantCulture)];

        return array;
    }

    private static bool IsArray<TValue>(IDictionary<string, TValue> dictionary)
    {
        if (dictionary.Count == 0)
            return false;

        for (var i = 0; i < dictionary.Count; i++)
        {
            if (!dictionary.ContainsKey(i.ToString(CultureInfo.InvariantCulture)))
                return false;
        }

        return true;
    }

    private async Task<IDictionary<string, string>> ResolveGroupValuesAsync(string language, string ns, string key, TranslationOptions options)
    {
        foreach (var (lookupLanguage, lookupNamespace) in GetLookupOrder(language, ns, options))
        {
            var tree = await ResolveTranslationTreeAsync(lookupLanguage, lookupNamespace).ConfigureAwait(false);

            if (tree is not IHierarchicalTranslationTree hierarchicalTree)
                continue;

            var values = hierarchicalTree.GetGroupValues(key);

            if (values != null)
                return values;
        }

        return null;
    }

    private static IEnumerable<(string Language, string Namespace)> GetLookupOrder(string language, string ns, TranslationOptions options)
    {
        yield return (language, ns);

        if (options?.FallbackNamespaces != null)
        {
            foreach (var fallbackNamespace in options.FallbackNamespaces)
                yield return (language, fallbackNamespace);
        }

        var fallbackLanguages = GetFallbackLanguages(language, options);

        if (fallbackLanguages == null)
            yield break;

        foreach (var fallbackLanguage in fallbackLanguages)
        {
            yield return (fallbackLanguage, ns);

            if (options.FallbackNamespaces == null)
                continue;

            foreach (var fallbackNamespace in options.FallbackNamespaces)
                yield return (fallbackLanguage, fallbackNamespace);
        }
    }

    private static void ValidateArguments(string language, string key, TranslationOptions options)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentNullException(nameof(language));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
    }

    private string SplitNamespace(ref string key, TranslationOptions options)
    {
        if (string.IsNullOrEmpty(NamespaceSeparator))
            return options.DefaultNamespace;

        var namespaceSeparatorIndex = key.IndexOf(NamespaceSeparator, StringComparison.Ordinal);

        if (namespaceSeparatorIndex < 0)
            return options.DefaultNamespace;

        var @namespace = key.Substring(0, namespaceSeparatorIndex);
        key = key.Substring(namespaceSeparatorIndex + NamespaceSeparator.Length);

        return @namespace;
    }

    private string GetDefaultValue(string language, IDictionary<string, object> args)
    {
        if (args == null || !args.Keys.Any(k => k.StartsWith("defaultValue", StringComparison.Ordinal)))
            return null;

        if (NeedsPluralHandling(language, args))
        {
            var suffixes = GetPluralSuffixes(language, args);

            if (suffixes.ZeroSuffix != null && args.TryGetValue($"defaultValue{suffixes.ZeroSuffix}", out var zeroDefault) && zeroDefault is string zeroDefaultValue)
                return zeroDefaultValue;

            if (args.TryGetValue($"defaultValue{suffixes.Suffix}", out var pluralDefault) && pluralDefault is string pluralDefaultValue)
                return pluralDefaultValue;

            if (suffixes.OrdinalFallbackSuffix != null && args.TryGetValue($"defaultValue{suffixes.OrdinalFallbackSuffix}", out var ordinalDefault) &&
                ordinalDefault is string ordinalDefaultValue)
                return ordinalDefaultValue;
        }

        return args.TryGetValue("defaultValue", out var defaultValue) ? defaultValue as string : null;
    }

    private bool NeedsPluralHandling(string language, IDictionary<string, object> args)
    {
        return CheckForSpecialArg(args, "count", typeof(int), typeof(long)) && _pluralResolver.NeedsPlural(language);
    }

    private PluralSuffixes GetPluralSuffixes(string language, IDictionary<string, object> args)
    {
        var count = (int)Convert.ChangeType(args["count"], typeof(int));

        if (_pluralResolver is not DefaultPluralResolver { JsonFormatVersion: JsonFormat.Version4 } pluralResolver)
            return new PluralSuffixes(_pluralResolver.GetPluralSuffix(language, count), null, null);

        if (args.TryGetValue("ordinal", out var ordinal) && ordinal is true)
        {
            var ordinalSuffix = pluralResolver.GetOrdinalPluralSuffix(language, count);
            var ordinalFallbackSuffix = $"{pluralResolver.PluralSeparator}{DefaultPluralResolver.GetOrdinalPluralCategory(language, count)}";

            return new PluralSuffixes(ordinalSuffix, null, ordinalFallbackSuffix);
        }

        var suffix = pluralResolver.GetPluralSuffix(language, count);
        string zeroSuffix = null;

        if (count == 0)
        {
            zeroSuffix = $"{pluralResolver.PluralSeparator}zero";

            if (zeroSuffix == suffix)
                zeroSuffix = null;
        }

        return new PluralSuffixes(suffix, zeroSuffix, null);
    }

    private static void AddPluralKeys(List<string> possibleKeys, string key, PluralSuffixes suffixes)
    {
        if (suffixes.OrdinalFallbackSuffix != null)
            possibleKeys.Add($"{key}{suffixes.OrdinalFallbackSuffix}");

        possibleKeys.Add($"{key}{suffixes.Suffix}");

        if (suffixes.ZeroSuffix != null)
            possibleKeys.Add($"{key}{suffixes.ZeroSuffix}");
    }

    private static bool CheckForSpecialArg(IDictionary<string, object> args, string key, params Type[] allowedTypes)
    {
        if (args == null)
            return false;

        if (!args.TryGetValue(key, out var value) || value == null)
            return false;

        var valueType = value.GetType();

        for (var i = 0; i < allowedTypes.Length; i++)
        {
            if (valueType == allowedTypes[i])
                return true;
        }

        return false;
    }

    private static bool IsEnabledByArg(IDictionary<string, object> args, string key)
    {
        return args == null || !args.TryGetValue(key, out var value) || value is true;
    }

    private async Task<string> ExtendTranslationAsync(string result, string key, string language, IDictionary<string, object> args,
        TranslationOptions options)
    {
        IDictionary<string, object> replaceArgs = args != null && args.TryGetValue("replace", out var replace) && replace != null && replace.GetType().IsClass
            ? replace.ToDictionary()
            : args;

        if (AllowInterpolation && IsEnabledByArg(args, "interpolate"))
            result = await _interpolator.InterpolateAsync(result, key, language, replaceArgs).ConfigureAwait(false);

        if (AllowNesting && IsEnabledByArg(args, "nest") && _interpolator.CanNest(result))
            result = await _interpolator.NestAsync(result, language, replaceArgs,
                (lang2, key2, args2) => TranslateAsync(lang2, key2, args2, options)).ConfigureAwait(false);

        if (AllowPostprocessing && PostProcessors.Count > 0)
            result = HandlePostProcessing(result, language, key, args);

        return result;
    }

    private string[] GetPostProcessorKeys(IDictionary<string, object> args)
    {
        if (args == null)
            return null;

        if (!args.TryGetValue("postProcess", out var localArgs))
            return null;

        if (localArgs is string postProcessorStr)
        {
            return postProcessorStr.IndexOf(',') > -1
                ? [.. postProcessorStr.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())]
                : [postProcessorStr];
        }

        return localArgs as string[];
    }

    private string HandlePostProcessing(string result, string language, string key, IDictionary<string, object> args)
    {
        var postProcessorKeys = GetPostProcessorKeys(args);

        if (postProcessorKeys != null)
            foreach (var postProcessorKey in postProcessorKeys)
            {
                if (string.IsNullOrWhiteSpace(postProcessorKey))
                    continue;

                foreach (var postProcessor in PostProcessors)
                {
                    if (postProcessor.Keyword == postProcessorKey)
                        result = postProcessor.ProcessResult(key, result, args, language, this);
                }
            }

        return result;
    }

    private async Task OnMissingKey(string language, string @namespace, string key, List<string> possibleKeys)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Missing translation for {namespace}:{key} in language {language}.", @namespace, key, language);

        if (MissingKey == null && MissingKeyHandlers.Count == 0)
            return;

        var args = new MissingKeyEventArgs(language, @namespace, key, [.. possibleKeys]);

        MissingKey?.Invoke(this, args);

        if (MissingKeyHandlers.Count > 0)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Invoking missing key handlers for {namespace}:{key} in language {language}.", @namespace, key, language);

            foreach (var missingKeyHandler in MissingKeyHandlers)
                await missingKeyHandler.HandleMissingKeyAsync(this, args).ConfigureAwait(false);
        }
    }

    private async Task<string> ResolveTranslationNoFallbackAsync(string language, string ns, string key, IDictionary<string, object> args,
        bool notifyMissingKey)
    {
        var translationTree = await ResolveTranslationTreeAsync(language, ns).ConfigureAwait(false);

        if (translationTree == null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Unable to resolve a translation tree for {ns} with language {language}", ns, language);

            return null;
        }

        var needsPluralHandling = NeedsPluralHandling(language, args);
        var needsContextHandling = CheckForSpecialArg(args, "context", typeof(string));

        var possibleKeys = new List<string>(needsPluralHandling || needsContextHandling ? 8 : 1) { key };
        var pluralSuffixes = default(PluralSuffixes);

        if (needsPluralHandling)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Translation {ns}:{key} needs plural handling.", ns, key);

            pluralSuffixes = GetPluralSuffixes(language, args);

            AddPluralKeys(possibleKeys, key, pluralSuffixes);
        }

        if (needsContextHandling)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Translation {ns}:{key} needs context handling.", ns, key);

            var contextKey = $"{key}{ContextSeparator}{(string)args["context"]}";
            possibleKeys.Add(contextKey);

            if (needsPluralHandling)
                AddPluralKeys(possibleKeys, contextKey, pluralSuffixes);
        }

        string result = null;
        var foundGroup = false;

        // Iterate over the possible keys starting with most specific pluralkey (-> contextkey only) -> singularkey only
        for (var i = possibleKeys.Count - 1; i >= 0; i--)
        {
            var currentKey = possibleKeys[i];

            try
            {
                result = translationTree.GetValue(currentKey, args);
            }
            catch (TranslationKeyInvalidException) when (translationTree is IHierarchicalTranslationTree hierarchicalTree &&
                                                         hierarchicalTree.GetGroupValues(currentKey) != null)
            {
                foundGroup = true;
            }

            if (result != null)
                break;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Unable to resolve a translation for {currentKey} from the translation tree.", currentKey);
        }

        if (result == null && notifyMissingKey && !foundGroup)
            await OnMissingKey(language, ns, key, possibleKeys).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("The resolved translation for {ns}:{key} on language {language} was \"{result}\"", ns, key, language, result);

        return result;
    }

    private async Task<string> ResolveTranslationAsync(string language, string ns, string key, IDictionary<string, object> args, TranslationOptions options,
        bool notifyMissingKey)
    {
        foreach (var (lookupLanguage, lookupNamespace) in GetLookupOrder(language, ns, options))
        {
            var result = await ResolveTranslationNoFallbackAsync(lookupLanguage, lookupNamespace, key, args, notifyMissingKey).ConfigureAwait(false);

            if (result != null)
                return result;
        }

        return null;
    }

    private static string[] GetFallbackLanguages(string language, TranslationOptions options)
    {
        if (options?.LanguageFallbacks != null && options.LanguageFallbacks.Count > 0)
        {
            if (options.LanguageFallbacks.TryGetValue(language, out var languageFallbacks))
                return languageFallbacks;

            if (options.LanguageFallbacks.TryGetValue(BackendUtilities.GetLanguagePart(language), out languageFallbacks))
                return languageFallbacks;
        }

        return options?.FallbackLanguages;
    }

    protected virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    private async Task<ITranslationTree> ResolveTranslationTreeAsync(string language, string ns)
    {
        var cacheKey = (language, ns);

        if (_treeCache.TryGetValue(cacheKey, out var cached) && (cached.ExpiresAt == DateTimeOffset.MaxValue || cached.ExpiresAt > UtcNow))
            return cached.Tree;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Trying to resolve translation tree {language}.{ns}", language, ns);

        var expiration = (_backend as IExpiringTranslationBackend)?.CacheExpiration;

        if (expiration == null)
            return _treeCache.GetOrAdd(cacheKey, new CachedTree(await _backend.LoadNamespaceAsync(language, ns).ConfigureAwait(false), DateTimeOffset.MaxValue)).Tree;

        ITranslationTree tree;

        try
        {
            tree = await _backend.LoadNamespaceAsync(language, ns).ConfigureAwait(false);
        }
        catch (Exception ex) when (cached != null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Reloading the translation tree {language}.{ns} failed, the expired tree is used.", language, ns);

            tree = cached.Tree;
        }

        _treeCache[cacheKey] = new CachedTree(tree, UtcNow + expiration.Value);

        return tree;
    }

    private sealed class CachedTree(ITranslationTree tree, DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;

        public ITranslationTree Tree { get; } = tree;
    }

    private readonly struct PluralSuffixes(string suffix, string zeroSuffix, string ordinalFallbackSuffix)
    {
        public string Suffix { get; } = suffix;

        public string ZeroSuffix { get; } = zeroSuffix;

        public string OrdinalFallbackSuffix { get; } = ordinalFallbackSuffix;
    }
}
