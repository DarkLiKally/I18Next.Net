using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Plugins
{
    public class DefaultTranslator : ITranslator
    {
        private readonly ITranslationBackend _backend;
        private readonly IInterpolator _interpolator;
        private readonly ILogger _logger;
        private readonly IPluralResolver _pluralResolver;

        private readonly ConcurrentDictionary<string, ITranslationTree> _treeCache = new ConcurrentDictionary<string, ITranslationTree>();

        public DefaultTranslator(ITranslationBackend backend, ILogger logger, IPluralResolver pluralResolver, IInterpolator interpolator)
        {
            _backend = backend;
            _logger = logger;
            _pluralResolver = pluralResolver;
            _interpolator = interpolator;
        }

        public bool AllowInterpolation { get; set; } = true;

        public bool AllowNesting { get; set; } = true;

        public bool AllowPostprocessing { get; set; } = true;

        public string ContextSeparator { get; set; } = "_";

        public List<IPostProcessor> PostProcessors { get; } = new List<IPostProcessor>();

        public virtual async Task<string> TranslateAsync(string language, string defaultNamespace, string key, IDictionary<string, object> args)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentNullException(nameof(language));
            if (string.IsNullOrWhiteSpace(defaultNamespace))
                throw new ArgumentNullException(nameof(defaultNamespace));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string actualNamespace;

            if (key.Contains(':'))
            {
                actualNamespace = key.Substring(0, key.IndexOf(':'));
                key = key.Substring(key.IndexOf(':') + 1);
            }
            else
            {
                actualNamespace = defaultNamespace;
            }

            if (language.ToLower() == "cimode")
                return $"{actualNamespace}:{key}";

            var result = await ResolveTranslationAsync(language, actualNamespace, key, args);

            if (result == null)
                return key;

            return await ExtendTranslationAsync(result, actualNamespace, key, language, args);
        }

        private async Task<string> ExtendTranslationAsync(string result, string ns, string key, string language, IDictionary<string, object> args)
        {
            IDictionary<string, object> replaceArgs;

            if ((args?.ContainsKey("replace") ?? false) && args["replace"].GetType().IsClass)
                replaceArgs = args["replace"].ToDictionary();
            else
                replaceArgs = args;

            if (AllowInterpolation && (!(args?.ContainsKey("interpolate") ?? false) || args["interpolate"] is bool interpolate && interpolate))
                result = await _interpolator.InterpolateAsync(result, key, language, replaceArgs);

            if (AllowNesting && (!(args?.ContainsKey("nest") ?? false) || args["nest"] is bool nest && nest) && _interpolator.CanNest(result))
                result = await _interpolator.NestAsync(result, language, replaceArgs, (lang2, key2, args2) => TranslateAsync(lang2, ns, key2, args2));

            if (AllowPostprocessing && PostProcessors.Count > 0)
                result = HandlePostProcessing(result, key, args);

            return result;
        }

        private string[] GetPostProcessorKeys(IDictionary<string, object> args)
        {
            if (args == null)
                return null;

            if (!args.ContainsKey("postProcess"))
                return null;

            if (args["postProcess"] is string postProcessor)
                return new[] { postProcessor };

            var localArgs = args["postProcess"];
            var postProcessType = localArgs.GetType();
            if (postProcessType.IsArray && postProcessType.HasElementType && postProcessType.GetElementType() == typeof(string))
                return localArgs as string[];

            return null;
        }

        private string HandlePostProcessing(string result, string key, IDictionary<string, object> args)
        {
            var postProcessorKeys = GetPostProcessorKeys(args);

            if (postProcessorKeys != null)
                foreach (var postProcessorKey in postProcessorKeys)
                {
                    if (string.IsNullOrWhiteSpace(postProcessorKey))
                        continue;

                    foreach (var postProcessor in PostProcessors)
                        if (postProcessor.Keyword == postProcessorKey)
                            result = postProcessor.Process(key, result, args);
                }

            return result;
        }

        private async Task<string> ResolveTranslationAsync(string language, string ns, string key, IDictionary<string, object> args)
        {
            var needsPluralHandling = (args?.ContainsKey("count") ?? false) && args["count"] is int && _pluralResolver.NeedsPlural(language);
            var needsContextHandling = (args?.ContainsKey("context") ?? false) && args["context"] is string;

            var finalKey = key;
            var possibleKeys = new Stack<string>();
            possibleKeys.Push(finalKey);
            var pluralSuffix = string.Empty;

            if (needsPluralHandling)
            {
                var count = (int) args["count"];
                pluralSuffix = _pluralResolver.GetPluralSuffix(language, count);

                // Fallback for plural if context was not found
                if (needsContextHandling)
                    possibleKeys.Push($"{finalKey}{pluralSuffix}");
            }

            // Get key for context if needed
            if (needsContextHandling)
            {
                var context = (string) args["context"];
                finalKey = $"{finalKey}{ContextSeparator}{context}";
                possibleKeys.Push(finalKey);
            }

            // Get key for plural if needed
            if (needsPluralHandling)
            {
                finalKey = $"{finalKey}{pluralSuffix}";
                possibleKeys.Push(finalKey);
            }

            var translationTree = await ResolveTranslationTreeAsync(language, ns);

            string result = null;
            // Iterate over the possible keys starting with most specific pluralkey (-> contextkey only) -> singularkey only
            while (possibleKeys.Count > 0)
            {
                var currentKey = possibleKeys.Pop();
                result = translationTree.GetValue(currentKey, args);

                if (result != null)
                    break;
            }

            return result;
        }

        private async Task<ITranslationTree> ResolveTranslationTreeAsync(string language, string ns)
        {
            var cacheKey = $"{language}.{ns}";

            if (_treeCache.TryGetValue(cacheKey, out var tree))
                return tree;

            tree = await _backend.LoadNamespaceAsync(language, ns);

            _treeCache.TryAdd(cacheKey, tree);

            return tree;
        }
    }
}
