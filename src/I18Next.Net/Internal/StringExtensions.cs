using System;

namespace I18Next.Net.Internal;

/// <summary>
///     String helpers used by the I18Next.Net packages, including <see cref="string.Split(string[], StringSplitOptions)" />
///     overloads with a single string separator for .NET Standard 2.0.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Replaces the first ordinal occurrence of a string.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="search">The string to replace.</param>
    /// <param name="replace">The replacement.</param>
    /// <returns>The text with the first occurrence replaced, or the unchanged text if it does not contain the string.</returns>
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        var pos = text.IndexOf(search, StringComparison.Ordinal);
        return pos < 0 ? text : text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    /// <summary>
    ///     Splits a string at a separator into at most the given number of parts.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <param name="splitter">The separator.</param>
    /// <param name="count">The maximum number of parts.</param>
    /// <param name="options">Whether to trim the parts or remove empty parts.</param>
    /// <returns>The parts.</returns>
    public static string[] Split(this string str, string splitter, int count, StringSplitOptions options)
    {
        return str.Split([splitter], count, options);
    }

    /// <summary>
    ///     Splits a string at a separator into at most the given number of parts.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <param name="splitter">The separator.</param>
    /// <param name="count">The maximum number of parts.</param>
    /// <returns>The parts.</returns>
    public static string[] Split(this string str, string splitter, int count)
    {
        return str.Split([splitter], count, StringSplitOptions.None);
    }

    /// <summary>
    ///     Splits a string at a separator.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <param name="splitter">The separator.</param>
    /// <returns>The parts.</returns>
    public static string[] Split(this string str, string splitter)
    {
        return str.Split([splitter], StringSplitOptions.None);
    }

    /// <summary>
    ///     Splits a string at a separator.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <param name="splitter">The separator.</param>
    /// <param name="options">Whether to trim the parts or remove empty parts.</param>
    /// <returns>The parts.</returns>
    public static string[] Split(this string str, string splitter, StringSplitOptions options)
    {
        return str.Split([splitter], options);
    }
}
