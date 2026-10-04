using System;

namespace I18Next.Net.Internal;

public static class StringExtensions
{
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        var pos = text.IndexOf(search, StringComparison.Ordinal);
        return pos < 0 ? text : text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    public static string[] Split(this string str, string splitter, int count, StringSplitOptions options)
    {
        return str.Split([splitter], count, options);
    }

    public static string[] Split(this string str, string splitter, int count)
    {
        return str.Split([splitter], count, StringSplitOptions.None);
    }

    public static string[] Split(this string str, string splitter)
    {
        return str.Split([splitter], StringSplitOptions.None);
    }

    public static string[] Split(this string str, string splitter, StringSplitOptions options)
    {
        return str.Split([splitter], options);
    }
}
