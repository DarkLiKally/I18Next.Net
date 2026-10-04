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

    public List<IMissingKeyHandler> MissingKeyHandlers { get; } = new();

    public event EventHandler<MissingKeyEventArgs> MissingKey;

    public List<IPostProcessor> PostProcessors { get; } = new();

    public virtual async Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentNullException(nameof(language));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        string actualNamespace;

        var namespaceSeparatorIndex = key.IndexOf(':');

        if (namespaceSeparatorIndex > -1)
        {
            actualNamespace = key.Substring(0, namespaceSeparatorIndex);
            key = key.Substring(namespaceSeparatorIndex + 1);
        }
        else
        {
            actualNamespace = options.DefaultNamespace;
        }

        if (string.Equals(language, "cimode", StringComparison.OrdinalIgnoreCase))
            return $"{actualNamespace}:{key}";

        var result = await ResolveTranslationAsync(language, actualNamespace, key, args, options).ConfigureAwait(false);

        if (result == null)
            return key;

        return await ExtendTranslationAsync(result, key, language, args, options).ConfigureAwait(false);
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

    private async Task<string> ResolveTranslationNoFallbackAsync(string language, string ns, string key, IDictionary<string, object> args)
    {
        var translationTree = await ResolveTranslationTreeAsync(language, ns).ConfigureAwait(false);

        if (translationTree == null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Unable to resolve a translation tree for {ns} with language {language}", ns, language);

            return null;
        }

        var needsPluralHandling = CheckForSpecialArg(args, "count", typeof(int), typeof(long)) && _pluralResolver.NeedsPlural(language);
        var needsContextHandling = CheckForSpecialArg(args, "context", typeof(string));

        var possibleKeys = new List<string>(needsPluralHandling || needsContextHandling ? 6 : 1) { key };
        var pluralSuffix = string.Empty;
        string zeroSuffix = null;

        if (needsPluralHandling)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Translation {ns}:{key} needs plural handling.", ns, key);

            var count = (int) Convert.ChangeType(args["count"], typeof(int));
            pluralSuffix = _pluralResolver.GetPluralSuffix(language, count);

            if (count == 0 && _pluralResolver is DefaultPluralResolver { JsonFormatVersion: JsonFormat.Version4 } pluralResolver)
            {
                zeroSuffix = $"{pluralResolver.PluralSeparator}zero";

                if (zeroSuffix == pluralSuffix)
                    zeroSuffix = null;
            }

            possibleKeys.Add($"{key}{pluralSuffix}");

            if (zeroSuffix != null)
                possibleKeys.Add($"{key}{zeroSuffix}");
        }

        if (needsContextHandling)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Translation {ns}:{key} needs context handling.", ns, key);

            var contextKey = $"{key}{ContextSeparator}{(string) args["context"]}";
            possibleKeys.Add(contextKey);

            if (needsPluralHandling)
            {
                possibleKeys.Add($"{contextKey}{pluralSuffix}");

                if (zeroSuffix != null)
                    possibleKeys.Add($"{contextKey}{zeroSuffix}");
            }
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
        
        if (result == null)
            await OnMissingKey(language, ns, key, possibleKeys).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("The resolved translation for {ns}:{key} on language {language} was \"{result}\"", ns, key, language, result);
        
        return result;
    }

    private async Task<string> ResolveTranslationAsync(string language, string ns, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        var result = await ResolveTranslationNoFallbackAsync(language, ns, key, args).ConfigureAwait(false);

        if (result == null && options?.FallbackNamespaces?.Length > 0)
        {
            foreach (var fallbackNamespace in options.FallbackNamespaces)
            {
                var fallbackResult = await ResolveTranslationNoFallbackAsync(language, fallbackNamespace, key, args).ConfigureAwait(false);
                if (fallbackResult != null)
                    return fallbackResult;
            }
        }

        if (result == null && options?.FallbackLanguages?.Length > 0)
        {
            foreach (var fallbackLanguage in options.FallbackLanguages)
            {
                var fallbackResult = await ResolveTranslationNoFallbackAsync(fallbackLanguage, ns, key, args).ConfigureAwait(false);
                if (fallbackResult != null)
                    return fallbackResult;
                
                if (options.FallbackNamespaces?.Length > 0)
                {
                    foreach (var fallbackNamespace in options.FallbackNamespaces)
                    {
                        fallbackResult = await ResolveTranslationNoFallbackAsync(fallbackLanguage, fallbackNamespace, key, args).ConfigureAwait(false);
                        if (fallbackResult != null)
                            return fallbackResult;
                    }
                }
            }
        }

        return result;
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
}
