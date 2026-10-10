using System;
using System.Collections.Generic;
using System.Text.Json;

using I18Next.Net.Backends;

namespace I18Next.Net.DataAnnotations;

internal static class DefaultValidationMessages
{
    private const string FallbackLanguage = "en";
    private const string ResourcePrefix = "I18Next.Net.DataAnnotations.Resources.";
    private const string ResourceSuffix = ".validation.json";

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Messages = new(Load);

    public static string Get(string language, string key)
    {
        return Find(language, key) ?? Find(BackendUtilities.GetLanguagePart(language), key) ?? Find(FallbackLanguage, key);
    }

    private static string Find(string language, string key)
    {
        return Messages.Value.TryGetValue(language, out var messages) && messages.TryGetValue(key, out var message) ? message : null;
    }

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var assembly = typeof(DefaultValidationMessages).Assembly;
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                continue;

            var language = name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - ResourceSuffix.Length);
            var messages = new Dictionary<string, string>();

            using (var stream = assembly.GetManifestResourceStream(name))
            using (var document = JsonDocument.Parse(stream))
                AddMessages(messages, null, document.RootElement);

            result[language] = messages;
        }

        return result;
    }

    private static void AddMessages(IDictionary<string, string> messages, string prefix, JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix == null ? property.Name : prefix + "." + property.Name;

            if (property.Value.ValueKind == JsonValueKind.Object)
                AddMessages(messages, key, property.Value);
            else
                messages[key] = property.Value.GetString();
        }
    }
}
