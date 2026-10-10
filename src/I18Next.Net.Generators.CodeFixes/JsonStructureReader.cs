using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace I18Next.Net.Generators.CodeFixes;

/// <summary>
///     Reads the objects and members of a JSON file with their positions, accepting comments and trailing commas like the generator.
/// </summary>
internal sealed class JsonStructureReader
{
    private readonly string _text;
    private int _position;

    private JsonStructureReader(string text)
    {
        _text = text;
    }

    public static JsonObjectNode Read(string text)
    {
        var reader = new JsonStructureReader(text);

        try
        {
            reader.SkipTrivia();

            if (reader.Peek() != '{')
                return null;

            var root = reader.ReadObject();
            reader.SkipTrivia();

            return reader._position < text.Length ? null : root;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private JsonObjectNode ReadObject()
    {
        var node = new JsonObjectNode(_position);
        _position++;

        while (true)
        {
            SkipTrivia();

            if (Peek() == '}')
            {
                node.End = _position;
                _position++;

                return node;
            }

            if (Peek() != '"')
                throw new FormatException();

            var start = _position;
            var name = ReadString();
            var nameEnd = _position;

            SkipTrivia();

            if (Peek() != ':')
                throw new FormatException();

            _position++;
            SkipTrivia();

            var member = new JsonMemberNode(name, start, nameEnd, _position);

            switch (Peek())
            {
                case '{':
                    member.Object = ReadObject();
                    break;
                case '[':
                    member.IsArray = true;
                    ReadArray();
                    break;
                default:
                    ReadScalar();
                    break;
            }

            member.ValueEnd = _position;
            node.Members.Add(member);

            SkipTrivia();

            if (Peek() == ',')
                _position++;
            else if (Peek() != '}')
                throw new FormatException();
        }
    }

    private void ReadArray()
    {
        _position++;

        while (true)
        {
            SkipTrivia();

            if (Peek() == ']')
            {
                _position++;
                return;
            }

            switch (Peek())
            {
                case '{':
                    ReadObject();
                    break;
                case '[':
                    ReadArray();
                    break;
                default:
                    ReadScalar();
                    break;
            }

            SkipTrivia();

            if (Peek() == ',')
                _position++;
            else if (Peek() != ']')
                throw new FormatException();
        }
    }

    private void ReadScalar()
    {
        if (Peek() == '"')
        {
            ReadString();
            return;
        }

        var start = _position;

        while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || "+-.".IndexOf(_text[_position]) >= 0))
            _position++;

        if (start == _position)
            throw new FormatException();
    }

    private string ReadString()
    {
        _position++;
        var builder = new StringBuilder();

        while (true)
        {
            if (_position >= _text.Length)
                throw new FormatException();

            var c = _text[_position++];

            if (c == '"')
                return builder.ToString();

            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (_position >= _text.Length)
                throw new FormatException();

            var escaped = _text[_position++];

            switch (escaped)
            {
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'u':
                    if (_position + 4 > _text.Length
                        || !int.TryParse(_text.Substring(_position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        throw new FormatException();

                    builder.Append((char)code);
                    _position += 4;
                    break;
                default:
                    builder.Append(escaped);
                    break;
            }
        }
    }

    private void SkipTrivia()
    {
        _position = JsonText.SkipTrivia(_text, _position);
    }

    private char Peek()
    {
        return _position < _text.Length ? _text[_position] : '\0';
    }
}

internal sealed class JsonObjectNode(int start)
{
    /// <summary>
    ///     The position of the closing brace.
    /// </summary>
    public int End { get; set; }

    public List<JsonMemberNode> Members { get; } = [];

    /// <summary>
    ///     The position of the opening brace.
    /// </summary>
    public int Start { get; } = start;
}

internal sealed class JsonMemberNode(string name, int start, int nameEnd, int valueStart)
{
    public bool IsArray { get; set; }

    public string Name { get; } = name;

    public int NameEnd { get; } = nameEnd;

    /// <summary>
    ///     The value if it is an object, otherwise <c>null</c>.
    /// </summary>
    public JsonObjectNode Object { get; set; }

    public int Start { get; } = start;

    public int ValueEnd { get; set; }

    public int ValueStart { get; } = valueStart;
}

internal static class JsonText
{
    public static int SkipTrivia(string text, int position)
    {
        while (position < text.Length)
        {
            var c = text[position];

            if (char.IsWhiteSpace(c) || c == '﻿')
            {
                position++;
            }
            else if (c == '/' && position + 1 < text.Length && text[position + 1] == '/')
            {
                while (position < text.Length && text[position] != '\n')
                    position++;
            }
            else if (c == '/' && position + 1 < text.Length && text[position + 1] == '*')
            {
                var end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                position = end < 0 ? text.Length : end + 2;
            }
            else
            {
                return position;
            }
        }

        return position;
    }
}
