using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private readonly ConcurrentDictionary<(string Language, string Namespace), ITranslationTree> _treeCache = new();

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

    public List<IMissingKeyHandler> MissingKeyHandlers { get; } = new();

    public event EventHandler<MissingKeyEventArgs> MissingKey;

    public List<IPostProcessor> PostProcessors { get; } = new();

    public virtual async Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        ValidateArguments(language, key, options);

        var actualNamespace = SplitNamespace(ref key, options);

        if (string.Equals(language, "cimode", StringComparison.OrdinalIgnoreCase))
            return $"{actualNamespace}{NamespaceSeparator}{key}";

        var result = await ResolveTranslationAsync(language, actualNamespace, key, args, options, true).ConfigureAwait(false)
                     ?? GetDefaultValue(language, args);

        if (result == null)
            return key;

        return await ExtendTranslationAsync(result, key, language, args, options).ConfigureAwait(false);
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
        var count = (int) Convert.ChangeType(args["count"], typeof(int));

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
        if (args == null || !args.TryGetValue(key, out var value))
            return true;

        return value is true;
    }

    private async Task<string> ExtendTranslationAsync(string result, string key, string language, IDictionary<string, object> args,
        TranslationOptions options)
    {
        IDictionary<string, object> replaceArgs;

        if (args != null && args.TryGetValue("replace", out var replace) && replace != null && replace.GetType().IsClass)
            replaceArgs = replace.ToDictionary();
        else
            replaceArgs = args;

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
            if (postProcessorStr.IndexOf(',') > -1)
                return postProcessorStr.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            
            return new[] { postProcessorStr };
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

        var args = new MissingKeyEventArgs(language, @namespace, key, possibleKeys.ToArray());

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

            var contextKey = $"{key}{ContextSeparator}{(string) args["context"]}";
            possibleKeys.Add(contextKey);

            if (needsPluralHandling)
                AddPluralKeys(possibleKeys, contextKey, pluralSuffixes);
        }

        string result = null;

        // Iterate over the possible keys starting with most specific pluralkey (-> contextkey only) -> singularkey only
        for (var i = possibleKeys.Count - 1; i >= 0; i--)
        {
            var currentKey = possibleKeys[i];
            result = translationTree.GetValue(currentKey, args);

            if (result != null)
                break;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Unable to resolve a translation for {currentKey} from the translation tree.", currentKey);
        }
        
        if (result == null && notifyMissingKey)
            await OnMissingKey(language, ns, key, possibleKeys).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("The resolved translation for {ns}:{key} on language {language} was \"{result}\"", ns, key, language, result);
        
        return result;
    }

    private async Task<string> ResolveTranslationAsync(string language, string ns, string key, IDictionary<string, object> args, TranslationOptions options,
        bool notifyMissingKey)
    {
        var result = await ResolveTranslationNoFallbackAsync(language, ns, key, args, notifyMissingKey).ConfigureAwait(false);

        if (result == null && options?.FallbackNamespaces?.Length > 0)
        {
            foreach (var fallbackNamespace in options.FallbackNamespaces)
            {
                var fallbackResult = await ResolveTranslationNoFallbackAsync(language, fallbackNamespace, key, args, notifyMissingKey).ConfigureAwait(false);
                if (fallbackResult != null)
                    return fallbackResult;
            }
        }

        var fallbackLanguages = GetFallbackLanguages(language, options);

        if (result == null && fallbackLanguages?.Length > 0)
        {
            foreach (var fallbackLanguage in fallbackLanguages)
            {
                var fallbackResult = await ResolveTranslationNoFallbackAsync(fallbackLanguage, ns, key, args, notifyMissingKey).ConfigureAwait(false);
                if (fallbackResult != null)
                    return fallbackResult;
                
                if (options.FallbackNamespaces?.Length > 0)
                {
                    foreach (var fallbackNamespace in options.FallbackNamespaces)
                    {
                        fallbackResult = await ResolveTranslationNoFallbackAsync(fallbackLanguage, fallbackNamespace, key, args, notifyMissingKey)
                            .ConfigureAwait(false);
                        if (fallbackResult != null)
                            return fallbackResult;
                    }
                }
            }
        }

        return result;
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

    private async Task<ITranslationTree> ResolveTranslationTreeAsync(string language, string ns)
    {
        var cacheKey = (language, ns);

        if (_treeCache.TryGetValue(cacheKey, out var tree))
            return tree;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Trying to resolve translation tree {language}.{ns}", language, ns);

        tree = await _backend.LoadNamespaceAsync(language, ns).ConfigureAwait(false);

        return _treeCache.GetOrAdd(cacheKey, tree);
    }

    private readonly struct PluralSuffixes
    {
        public PluralSuffixes(string suffix, string zeroSuffix, string ordinalFallbackSuffix)
        {
            Suffix = suffix;
            ZeroSuffix = zeroSuffix;
            OrdinalFallbackSuffix = ordinalFallbackSuffix;
        }

        public string Suffix { get; }

        public string ZeroSuffix { get; }

        public string OrdinalFallbackSuffix { get; }
    }
}
