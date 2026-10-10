using System;
using System.Collections.Generic;
using System.Text.Json;

namespace I18Next.Net.Extensions.Configuration;

public class I18NextOptions
{
    public string DefaultLanguage { get; set; } = "en-US";

    public string DefaultNamespace { get; set; } = "translation";

    public bool DetectLanguageOnEachTranslation { get; set; }

    public IList<string> FallbackLanguages { get; set; } = [];

    public IList<string> FallbackNamespaces { get; set; } = [];

    /// <summary>
    ///     The options used to map translations to models. Set options with a source generated JSON serializer context for
    ///     trimmed and Native AOT applications.
    /// </summary>
    public JsonSerializerOptions ModelSerializerOptions { get; set; }

    public IDictionary<string, string[]> LanguageFallbacks { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}
