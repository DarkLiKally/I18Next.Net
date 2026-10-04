using System;
using System.Collections.Generic;

namespace I18Next.Net.Extensions.Configuration;

public class I18NextOptions
{
    public string DefaultLanguage { get; set; } = "en-US";

    public string DefaultNamespace { get; set; } = "translation";

    public bool DetectLanguageOnEachTranslation { get; set; }

    public IList<string> FallbackLanguages { get; set; } = [];

    public IList<string> FallbackNamespaces { get; set; } = [];

    public IDictionary<string, string[]> LanguageFallbacks { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}
