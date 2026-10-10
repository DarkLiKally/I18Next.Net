using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     Finds the interpolations (<c>{{name}}</c>) and nestings (<c>$t(key)</c>) of a translation.
/// </summary>
internal static class Placeholders
{
    private static readonly Regex InterpolationPattern = new(@"\{\{(.+?)\}\}", RegexOptions.CultureInvariant);

    private static readonly Regex NestingPattern = new(@"\$t\(\s*(?<key>[^,)]+)", RegexOptions.CultureInvariant);

    /// <summary>
    ///     Gets the normalized placeholders like <c>{{name}}</c> or <c>$t(key)</c>, without formats and options.
    /// </summary>
    public static ISet<string> Get(string text)
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(text))
            return result;

        foreach (Match match in InterpolationPattern.Matches(text))
        {
            var name = match.Groups[1].Value.Split(',')[0].Trim();

            if (name.StartsWith("-", StringComparison.Ordinal))
                name = name.Substring(1).TrimStart();

            if (name.Length > 0)
                result.Add("{{" + name + "}}");
        }

        foreach (var key in GetNestedKeys(text))
            result.Add("$t(" + key + ")");

        return result;
    }

    /// <summary>
    ///     Gets the keys referenced by <c>$t(key)</c>, including a namespace prefix.
    /// </summary>
    public static IEnumerable<string> GetNestedKeys(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        foreach (Match match in NestingPattern.Matches(text))
        {
            var key = match.Groups["key"].Value.Trim().Trim('"', '\'');

            if (key.Length > 0)
                yield return key;
        }
    }
}
