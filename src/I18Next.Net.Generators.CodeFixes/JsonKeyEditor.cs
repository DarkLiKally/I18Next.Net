using System;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.Text;

namespace I18Next.Net.Generators.CodeFixes;

internal static class JsonKeyEditor
{
    private const string DefaultIndent = "  ";
    private const string DefaultColon = ": ";

    /// <summary>
    ///     Returns the change adding a key to a JSON file, or <c>null</c> when the key exists, a part of it is not an object or the file is invalid.
    ///     The key is added like the keys of the reference file are ordered, else in alphabetical order when the object is sorted, else at the end.
    /// </summary>
    public static TextChange? AddKey(string text, string key, string value, string referenceText = null)
    {
        var segments = key.Split('.');

        if (segments.Any(s => s.Length == 0))
            return null;

        var root = JsonStructureReader.Read(text);

        if (root == null)
            return null;

        var parent = root;
        var index = 0;

        while (true)
        {
            var (member, end) = FindMember(parent, segments, index, segments.Length);

            if (member == null)
                break;

            if (end == segments.Length || member.Object == null)
                return null;

            parent = member.Object;
            index = end;
        }

        var flat = parent.Members.Any(m => m.Name.IndexOf('.') >= 0);
        var name = flat ? string.Join(".", segments, index, segments.Length - index) : segments[index];
        var nested = flat ? [] : segments.Skip(index + 1).ToArray();
        var position = GetPosition(parent, name, segments, index, referenceText);

        return Insert(text, root, parent, name, nested, value, position);
    }

    /// <summary>
    ///     Returns whether a part of the key is an array in the JSON file.
    /// </summary>
    public static bool IsInArray(string text, string key)
    {
        var segments = key.Split('.');
        var node = JsonStructureReader.Read(text);
        var index = 0;

        while (node != null && index < segments.Length)
        {
            var (member, end) = FindMember(node, segments, index, segments.Length);

            if (member == null)
                return false;

            if (member.IsArray)
                return true;

            node = member.Object;
            index = end;
        }

        return false;
    }

    private static (JsonMemberNode Member, int End) FindMember(JsonObjectNode node, string[] segments, int index, int length)
    {
        for (var end = length; end > index; end--)
        {
            var name = string.Join(".", segments, index, end - index);
            var member = node.Members.LastOrDefault(m => m.Name == name);

            if (member != null)
                return (member, end);
        }

        return (null, index);
    }

    private static JsonObjectNode FindObject(JsonObjectNode node, string[] segments, int length)
    {
        var index = 0;

        while (node != null && index < length)
        {
            var (member, end) = FindMember(node, segments, index, length);
            node = member?.Object;
            index = end;
        }

        return node;
    }

    private static int GetPosition(JsonObjectNode parent, string name, string[] segments, int index, string referenceText)
    {
        var members = parent.Members;
        var reference = referenceText == null ? null : FindObject(JsonStructureReader.Read(referenceText), segments, index);
        var referenceIndex = reference?.Members.FindIndex(m => m.Name == name) ?? -1;

        if (referenceIndex >= 0)
        {
            for (var i = referenceIndex - 1; i >= 0; i--)
            {
                var existing = members.FindLastIndex(m => m.Name == reference.Members[i].Name);

                if (existing >= 0)
                    return existing + 1;
            }

            for (var i = referenceIndex + 1; i < reference.Members.Count; i++)
            {
                var existing = members.FindIndex(m => m.Name == reference.Members[i].Name);

                if (existing >= 0)
                    return existing;
            }
        }

        if (members.Count < 2 || members.Zip(members.Skip(1), (a, b) => string.CompareOrdinal(a.Name, b.Name) <= 0).Any(sorted => !sorted))
            return members.Count;

        var next = members.FindIndex(m => string.CompareOrdinal(m.Name, name) > 0);

        return next < 0 ? members.Count : next;
    }

    private static TextChange Insert(string text, JsonObjectNode root, JsonObjectNode parent, string name, string[] nested, string value, int position)
    {
        var newLine = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
        var members = parent.Members;
        var multiline = members.Count > 0 ? ContainsLineBreak(text, parent) : parent == root || ContainsLineBreak(text, root);
        var indent = GetIndentUnit(text, root);
        var memberIndent = members.Count > 0 && StartsLine(text, members[0].Start)
            ? GetLineIndent(text, members[0].Start)
            : GetLineIndent(text, parent.Start) + indent;
        var colon = GetColon(text, root);
        var member = Quote(name) + colon + Render(nested, 0, value, memberIndent, indent, colon, multiline ? newLine : null);

        if (members.Count == 0)
        {
            var inner = TextSpan.FromBounds(parent.Start + 1, parent.End);

            if (text.Substring(inner.Start, inner.Length).Trim().Length == 0)
                return new TextChange(inner, multiline ? newLine + memberIndent + member + newLine + GetLineIndent(text, parent.Start) : " " + member + " ");

            return new TextChange(new TextSpan(inner.Start, 0), multiline ? newLine + memberIndent + member : " " + member);
        }

        if (position < members.Count)
        {
            var next = members[position];

            if (multiline && StartsLine(text, next.Start))
                return new TextChange(new TextSpan(GetLineStart(text, next.Start), 0), memberIndent + member + "," + newLine);

            return new TextChange(new TextSpan(next.Start, 0), member + ", ");
        }

        var last = members[members.Count - 1];
        var separator = JsonText.SkipTrivia(text, last.ValueEnd);

        if (text[separator] == ',')
        {
            if (multiline && !IsSameLine(text, separator, parent.End))
                return new TextChange(new TextSpan(GetLineEnd(text, separator), 0), newLine + memberIndent + member + ",");

            return new TextChange(new TextSpan(separator + 1, 0), " " + member + ",");
        }

        var lineEnd = GetLineEnd(text, last.ValueEnd);
        var rest = text.Substring(last.ValueEnd, lineEnd - last.ValueEnd);

        if (multiline && !IsSameLine(text, last.ValueEnd, parent.End) && rest.IndexOf("/*", StringComparison.Ordinal) < 0)
            return new TextChange(TextSpan.FromBounds(last.ValueEnd, lineEnd), "," + rest + newLine + memberIndent + member);

        return new TextChange(new TextSpan(last.ValueEnd, 0), ", " + member);
    }

    private static string Render(string[] names, int index, string value, string indent, string unit, string colon, string newLine)
    {
        if (index == names.Length)
            return Quote(value);

        var member = Quote(names[index]) + colon + Render(names, index + 1, value, indent + unit, unit, colon, newLine);

        return newLine == null ? "{ " + member + " }" : "{" + newLine + indent + unit + member + newLine + indent + "}";
    }

    private static string GetIndentUnit(string text, JsonObjectNode root)
    {
        if (root.Members.Count == 0 || !StartsLine(text, root.Members[0].Start))
            return DefaultIndent;

        var rootIndent = GetLineIndent(text, root.Start);
        var memberIndent = GetLineIndent(text, root.Members[0].Start);

        return memberIndent.Length > rootIndent.Length && memberIndent.StartsWith(rootIndent, StringComparison.Ordinal)
            ? memberIndent.Substring(rootIndent.Length)
            : DefaultIndent;
    }

    private static string GetColon(string text, JsonObjectNode root)
    {
        if (root.Members.Count == 0)
            return DefaultColon;

        var first = root.Members[0];
        var colon = text.Substring(first.NameEnd, first.ValueStart - first.NameEnd);

        return colon.Trim() == ":" && colon.IndexOf('\n') < 0 && colon.IndexOf('\r') < 0 ? colon : DefaultColon;
    }

    private static bool ContainsLineBreak(string text, JsonObjectNode node)
    {
        return text.IndexOf('\n', node.Start, node.End - node.Start) >= 0;
    }

    private static bool IsSameLine(string text, int start, int end)
    {
        return text.IndexOf('\n', start, end - start) < 0;
    }

    private static bool StartsLine(string text, int position)
    {
        var lineStart = GetLineStart(text, position);

        return text.Substring(lineStart, position - lineStart).Trim().Length == 0;
    }

    private static int GetLineStart(string text, int position)
    {
        return position == 0 ? 0 : text.LastIndexOf('\n', position - 1) + 1;
    }

    private static int GetLineEnd(string text, int position)
    {
        var end = text.IndexOfAny(['\r', '\n'], position);

        return end < 0 ? text.Length : end;
    }

    private static string GetLineIndent(string text, int position)
    {
        var start = GetLineStart(text, position);
        var end = start;

        while (end < position && (text[end] == ' ' || text[end] == '\t'))
            end++;

        return text.Substring(start, end - start);
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (c < ' ')
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);
                    break;
            }
        }

        return builder.Append('"').ToString();
    }
}
