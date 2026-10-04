using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace I18Next.Net.Generators;

internal sealed class ResourceFile : IEquatable<ResourceFile>
{
    private ResourceFile(string path, string text)
    {
        Path = path;
        Text = text;

        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/');

        Namespace = parts[parts.Length - 1].Substring(0, parts[parts.Length - 1].Length - ".json".Length);
        Language = parts.Length > 1 ? parts[parts.Length - 2] : "";
        Directory = parts.Length > 2 ? string.Join("/", parts, 0, parts.Length - 2) : "";

        try
        {
            Entries = JsonResourceReader.Read(text);
        }
        catch (JsonResourceException ex)
        {
            Entries = [];
            Error = ex;
        }
    }

    public string Directory { get; }

    public IReadOnlyList<ResourceEntry> Entries { get; }

    public JsonResourceException Error { get; }

    public string Language { get; }

    public string Namespace { get; }

    public string Path { get; }

    public string Text { get; }

    public static ResourceFile Create(AdditionalText file, CancellationToken cancellationToken)
    {
        if (!file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return null;

        var text = file.GetText(cancellationToken);

        return text == null ? null : new ResourceFile(file.Path, text.ToString());
    }

    public bool IsIn(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');

        if (normalized.StartsWith("./", StringComparison.Ordinal))
            normalized = normalized.Substring(2);

        return Directory.Equals(normalized, StringComparison.OrdinalIgnoreCase)
               || Directory.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase);
    }

    public Location GetLocation(int line = 0, int column = 0)
    {
        var position = new LinePosition(line, column);

        return Location.Create(Path, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    public bool Equals(ResourceFile other)
    {
        return other != null && Path == other.Path && Text == other.Text;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ResourceFile);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode() ^ Text.GetHashCode();
    }
}
