using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Plugins;

using Jeffijoe.MessageFormat;

namespace I18Next.Net.ICU;

/// <summary>
///     An interpolator for translations in ICU message format, e.g.
///     <c>{count, plural, one {# item} other {# items}}</c>, using Jeffijoe.MessageFormat. Plurals and selects are
///     resolved by the message format, nesting and formatter plugins are not supported.
/// </summary>
/// <param name="useCache">Whether the message formatters cache the parsed patterns.</param>
public class MessageFormatInterpolator(bool useCache) : IInterpolator
{
    private readonly ConcurrentDictionary<string, MessageFormatter> _messageFormatters = new();
    private readonly bool _useCache = useCache;

    /// <summary>
    ///     Creates an interpolator whose message formatters cache the parsed patterns.
    /// </summary>
    public MessageFormatInterpolator()
        : this(true)
    {
    }

    /// <summary>
    ///     Returns <c>false</c> because nesting is not supported.
    /// </summary>
    /// <param name="source">The interpolated translation.</param>
    /// <returns>Always <c>false</c>.</returns>
    public bool CanNest(string source)
    {
        return false;
    }

    /// <summary>
    ///     Not supported, the ICU message format formats the values itself.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public List<IFormatter> Formatters => throw new NotSupportedException("Formatters are not supported by the ICU message format interpolator.");

    /// <summary>
    ///     Formats a translation in ICU message format with the message formatter of the language.
    /// </summary>
    /// <param name="source">The translation in ICU message format.</param>
    /// <param name="key">The key of the translation. Ignored.</param>
    /// <param name="language">The language whose plural rules and formatter are used.</param>
    /// <param name="args">The values of the arguments in the message, may be <c>null</c>.</param>
    /// <returns>The formatted translation or <c>null</c> if <paramref name="source" /> is <c>null</c>.</returns>
    public Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
    {
        if (source == null)
            return Task.FromResult((string)null);

        var messageFormatter = EnsureMessageFormatter(language);

        return Task.FromResult(messageFormatter.FormatMessage(source, args));
    }

    /// <summary>
    ///     Not supported, <see cref="CanNest" /> always returns <c>false</c>.
    /// </summary>
    /// <param name="source">The interpolated translation.</param>
    /// <param name="language">The language of the translation.</param>
    /// <param name="args">The arguments of the translation.</param>
    /// <param name="translateAsync">Translates the nested keys.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync)
    {
        throw new NotSupportedException("Nesting is not supported by the ICU message format interpolator.");
    }

    /// <summary>
    ///     Configures the message formatter of a language, e.g. to add custom formatters or pluralizers. The formatter is
    ///     created if it does not exist yet.
    /// </summary>
    /// <param name="language">The language whose formatter is configured.</param>
    /// <param name="formatter">Configures the formatter, may be <c>null</c>.</param>
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
