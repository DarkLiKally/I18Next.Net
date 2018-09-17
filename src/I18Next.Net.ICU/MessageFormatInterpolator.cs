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
        private readonly ConcurrentDictionary<string, MessageFormatter> _messageFormatters = new ConcurrentDictionary<string, MessageFormatter>();
        private readonly bool _useCache;

        public MessageFormatInterpolator()
            : this(true)
        {
        }

        public MessageFormatInterpolator(bool useCache)
        {
            _useCache = useCache;
        }

        public bool CanNest(string source)
        {
            return false;
        }

        public List<IFormatter> Formatters => throw new NotSupportedException("Formatters are not supported by the ICU message format interpolator.");

        public Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
        {
            if (source == null)
                return Task.FromResult((string) null);

            var messageFormatter = EnsureMessageFormatter(language);

            return Task.FromResult(messageFormatter.FormatMessage(source, args));
        }

        public Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync)
        {
            throw new NotSupportedException("Nesting is not supported by the ICU message format interpolator.");
        }

        public void ConfigureMessageFormatter(string language, Action<MessageFormatter> formatter)
        {
            var messageFormatter = EnsureMessageFormatter(language);

            formatter?.Invoke(messageFormatter);
        }

        private MessageFormatter EnsureMessageFormatter(string language)
        {
            if (!_messageFormatters.TryGetValue(language, out var messageFormatter))
                _messageFormatters.TryAdd(language, messageFormatter = new MessageFormatter(_useCache, language));

            return messageFormatter;
        }
    }
}
