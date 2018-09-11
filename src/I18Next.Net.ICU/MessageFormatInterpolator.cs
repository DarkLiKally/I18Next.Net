using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using I18Next.Net.Plugins;
using Jeffijoe.MessageFormat;

namespace I18Next.Net.ICU
{
    public class MessageFormatInterpolator : IInterpolator
    {
        private readonly bool _useCache;
        private readonly ConcurrentDictionary<string, MessageFormatter> _formatters = new ConcurrentDictionary<string, MessageFormatter>();

        public MessageFormatInterpolator(bool useCache)
        {
            _useCache = useCache;
        }

        public bool CanNest(string source)
        {
            return false;
        }

        public Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
        {
            if (source == null)
                return Task.FromResult((string) null);

            if (!_formatters.TryGetValue(language, out var messageFormatter))
                _formatters.TryAdd(language, messageFormatter = new MessageFormatter(_useCache, language));

            return Task.FromResult(messageFormatter.FormatMessage(source, args));
        }

        public Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync)
        {
            throw new NotSupportedException("Nesting is not supported by the ICU message format interpolator.");
        }
    }
}
