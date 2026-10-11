using System;
using System.Collections.Generic;
using System.Text.Json;

namespace I18Next.Net.Extensions.Configuration;

/// <summary>
///     The options of the <see cref="II18Next" /> instances registered with <see cref="ServiceCollectionExtensions" />,
///     configured with the <see cref="Builder.I18NextBuilder" /> or bound from configuration.
/// </summary>
public class I18NextOptions
{
    /// <summary>
    ///     The language the instances translate into, <c>en-US</c> by default, like the i18next <c>lng</c> option. The
    ///     default language detector returns it too.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    ///     The namespace used for keys without namespace prefix, <c>translation</c> by default, like the i18next
    ///     <c>defaultNS</c> option.
    /// </summary>
    public string DefaultNamespace { get; set; } = "translation";

    /// <summary>
    ///     Whether the language detector is asked for the language of each translation instead of using
    ///     <see cref="DefaultLanguage" />, e.g. to translate into the language of the current request.
    /// </summary>
    public bool DetectLanguageOnEachTranslation { get; set; }

    /// <summary>
    ///     The languages looked into in the given order when a key is missing in the requested language, like the i18next
    ///     <c>fallbackLng</c> option. Empty by default.
    /// </summary>
    public IList<string> FallbackLanguages { get; set; } = [];

    /// <summary>
    ///     The namespaces looked into in the given order when a key is missing in the requested namespace, like the
    ///     i18next <c>fallbackNS</c> option. Empty by default.
    /// </summary>
    public IList<string> FallbackNamespaces { get; set; } = [];

    /// <summary>
    ///     The options used to map translations to models. Set options with a source generated JSON serializer context for
    ///     trimmed and Native AOT applications.
    /// </summary>
    public JsonSerializerOptions ModelSerializerOptions { get; set; }

    /// <summary>
    ///     Fallback languages for specific languages, keyed case-insensitively by language. They take precedence over
    ///     <see cref="FallbackLanguages" />, and regional languages (e.g. <c>de-CH</c>) use the entry of their language
    ///     part (e.g. <c>de</c>) when they have none.
    /// </summary>
    public IDictionary<string, string[]> LanguageFallbacks { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}
