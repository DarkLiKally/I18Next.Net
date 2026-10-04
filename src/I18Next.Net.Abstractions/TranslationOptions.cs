using System;
using System.Collections.Generic;

namespace I18Next.Net;

public class TranslationOptions
{
    public string DefaultNamespace
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(value));

            field = value;
        }
    }

    public string[] FallbackLanguages { get; set; }

    public string[] FallbackNamespaces { get; set; }

    public IDictionary<string, string[]> LanguageFallbacks { get; set; }
}
