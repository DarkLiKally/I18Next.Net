#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace I18Next.Net.AspNetCore.Internal;

internal sealed class ResourceNameFilter
{
    private const int MaxLanguageLength = 35;
    private const int MaxNamespaceLength = 100;

    private readonly Dictionary<string, string> _languages;
    private readonly HashSet<string> _namespaces;

    public ResourceNameFilter(I18NextEndpointOptions options)
    {
        if (options.Languages is { Count: > 0 })
        {
            _languages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var language in options.Languages)
            {
                if (!IsValidLanguage(language))
                    throw new ArgumentException($"The language `{language}` is not a valid language.", nameof(options));

                _languages[language] = language;
            }
        }

        if (options.Namespaces is { Count: > 0 })
        {
            _namespaces = new HashSet<string>(StringComparer.Ordinal);

            foreach (var ns in options.Namespaces)
            {
                if (!IsValidNamespace(ns))
                    throw new ArgumentException($"The namespace `{ns}` is not a valid namespace.", nameof(options));

                _namespaces.Add(ns);
            }
        }
    }

    public string GetLanguage(string language)
    {
        if (_languages != null)
            return language != null && _languages.TryGetValue(language, out var configuredLanguage) ? configuredLanguage : null;

        return IsValidLanguage(language) ? language : null;
    }

    public string GetNamespace(string ns)
    {
        if (_namespaces != null)
            return ns != null && _namespaces.Contains(ns) ? ns : null;

        return IsValidNamespace(ns) ? ns : null;
    }

    private static bool IsValidLanguage(string language)
    {
        if (string.IsNullOrEmpty(language) || language.Length > MaxLanguageLength || !IsAsciiLetter(language[0]))
            return false;

        foreach (var c in language)
        {
            if (!IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
                return false;
        }

        return true;
    }

    private static bool IsValidNamespace(string ns)
    {
        if (string.IsNullOrEmpty(ns) || ns.Length > MaxNamespaceLength || ns[0] == '.' || ns[ns.Length - 1] == '.')
            return false;

        for (var i = 0; i < ns.Length; i++)
        {
            var c = ns[i];

            if (c == '.' && ns[i - 1] == '.')
                return false;

            if (!IsAsciiLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                return false;
        }

        return true;
    }

    private static bool IsAsciiLetter(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    private static bool IsAsciiLetterOrDigit(char c)
    {
        return IsAsciiLetter(c) || c is >= '0' and <= '9';
    }
}
#endif
