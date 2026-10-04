using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace I18Next.Net.Generators;

internal sealed class JsonResourceReader
{
    private readonly List<ResourceEntry> _entries = [];
    private readonly string _text;
    private int _position;

    private JsonResourceReader(string text)
    {
        _text = text;
    }

    public static IReadOnlyList<ResourceEntry> Read(string text)
    {
        var reader = new JsonResourceReader(text);

        reader.SkipWhitespace();

        if (reader.Peek() != '{')
            throw reader.Error("Expected '{' at the start of the file");

        reader.ReadObject("");
        reader.SkipWhitespace();

        if (reader._position < text.Length)
            throw reader.Error("Unexpected content after the end of the root object");

        return reader._entries;
    }

    private void ReadValue(string key)
    {
        SkipWhitespace();

        switch (Peek())
        {
            case '{':
                ReadObject(key);
                break;
            case '[':
                ReadArray(key);
                break;
            case '"':
                _entries.Add(new ResourceEntry(key, ReadString()));
                break;
            case 't':
                Expect("true");
                _entries.Add(new ResourceEntry(key, "True"));
                break;
            case 'f':
                Expect("false");
                _entries.Add(new ResourceEntry(key, "False"));
                break;
            case 'n':
                Expect("null");
                break;
            default:
                _entries.Add(new ResourceEntry(key, ReadNumber()));
                break;
        }
    }

    private void ReadObject(string path)
    {
        _position++;

        while (true)
        {
            SkipWhitespace();

            if (Peek() == '}')
            {
                _position++;
                return;
            }

            if (Peek() != '"')
                throw Error("Expected a property name");

            var name = ReadString();

            SkipWhitespace();

            if (Peek() != ':')
                throw Error("Expected ':' after the property name");

            _position++;
            ReadValue(path.Length == 0 ? name : path + "." + name);

            if (!ReadSeparator('}'))
                return;
        }
    }

    private void ReadArray(string path)
    {
        _position++;
        var index = 0;

        while (true)
        {
            SkipWhitespace();

            if (Peek() == ']')
            {
                _position++;
                return;
            }

            ReadValue(path + "." + index.ToString(CultureInfo.InvariantCulture));
            index++;

            if (!ReadSeparator(']'))
                return;
        }
    }

    private bool ReadSeparator(char end)
    {
        SkipWhitespace();

        var c = Peek();

        if (c == ',')
        {
            _position++;
            return true;
        }

        if (c == end)
        {
            _position++;
            return false;
        }

        throw Error($"Expected ',' or '{end}'");
    }

    private string ReadString()
    {
        _position++;
        var builder = new StringBuilder();

        while (true)
        {
            if (_position >= _text.Length)
                throw Error("Unterminated string");

            var c = _text[_position++];

            if (c == '"')
                return builder.ToString();

            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (_position >= _text.Length)
                throw Error("Unterminated string");

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
                        throw Error("Invalid unicode escape sequence");

                    builder.Append((char)code);
                    _position += 4;
                    break;
                default:
                    builder.Append(escaped);
                    break;
            }
        }
    }

    private string ReadNumber()
    {
        var start = _position;

        while (_position < _text.Length && "+-0123456789.eE".IndexOf(_text[_position]) >= 0)
            _position++;

        if (start == _position)
            throw Error("Unexpected character");

        return _text.Substring(start, _position - start);
    }

    private void Expect(string literal)
    {
        if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            throw Error("Unexpected character");

        _position += literal.Length;
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length)
        {
            var c = _text[_position];

            if (char.IsWhiteSpace(c) || c == '﻿')
            {
                _position++;
            }
            else if (c == '/' && _position + 1 < _text.Length && _text[_position + 1] == '/')
            {
                while (_position < _text.Length && _text[_position] != '\n')
                    _position++;
            }
            else if (c == '/' && _position + 1 < _text.Length && _text[_position + 1] == '*')
            {
                var end = _text.IndexOf("*/", _position + 2, StringComparison.Ordinal);
                _position = end < 0 ? _text.Length : end + 2;
            }
            else
            {
                return;
            }
        }
    }

    private char Peek()
    {
        return _position < _text.Length ? _text[_position] : '\0';
    }

    private JsonResourceException Error(string message)
    {
        var line = 0;
        var column = 0;

        for (var i = 0; i < _position && i < _text.Length; i++)
        {
            if (_text[i] == '\n')
            {
                line++;
                column = 0;
            }
            else
            {
                column++;
            }
        }

        return new JsonResourceException(message, line, column);
    }
}

internal sealed class JsonResourceException(string message, int line, int column) : Exception(message)
{
    public int Column { get; } = column;

    public int Line { get; } = line;
}

internal readonly struct ResourceEntry(string key, string value)
{
    public string Key { get; } = key;

    public string Value { get; } = value;
}
