using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins;

/// <summary>
///     Writes missing keys into JSON files like the <c>saveMissing</c> option of the i18next-fs-backend, so they can be
///     translated later. Meant for development.
/// </summary>
public class FileMissingKeyHandler(string addPath = FileMissingKeyHandler.DefaultAddPath) : IMissingKeyHandler
{
    public const string DefaultAddPath = "locales/{{lng}}/{{ns}}.missing.json";

    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly ConcurrentDictionary<(string Language, string Namespace, string Key), bool> _handledKeys = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    ///     The file the missing keys are written to. <c>{{lng}}</c> and <c>{{ns}}</c> are replaced with the language and the
    ///     namespace.
    /// </summary>
    public string AddPath { get; } = addPath ?? throw new ArgumentNullException(nameof(addPath));

    /// <summary>
    ///     Separates the levels of nested keys. <c>null</c> writes the keys flat.
    /// </summary>
    public string KeySeparator { get; set; } = ".";

    public async Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        if (!IsValidPathSegment(args.Language) || !IsValidPathSegment(args.Namespace) || !_handledKeys.TryAdd((args.Language, args.Namespace, args.Key), true))
            return;

        var path = AddPath.Replace("{{lng}}", args.Language).Replace("{{ns}}", args.Namespace);

        await _lock.WaitAsync().ConfigureAwait(false);

        try
        {
            var root = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [] : [];

            if (!AddValue(root, args.Key, args.DefaultValue ?? args.Key))
                return;

            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new Utf8JsonWriter(stream, WriterOptions);

            root.WriteTo(writer);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static bool IsValidPathSegment(string value)
    {
        return !string.IsNullOrEmpty(value) && value != "." && value != ".." && value.IndexOfAny(['/', '\\', ':']) < 0 &&
               value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private bool AddValue(JsonObject root, string key, string value)
    {
        if (root.ContainsKey(key))
            return false;

        if (string.IsNullOrEmpty(KeySeparator) || key.IndexOf(KeySeparator, StringComparison.Ordinal) < 0)
        {
            root[key] = value;

            return true;
        }

        var parts = key.Split([KeySeparator], StringSplitOptions.None);
        var current = root;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!current.TryGetPropertyValue(parts[i], out var child))
            {
                child = new JsonObject();
                current[parts[i]] = child;
            }

            if (child is not JsonObject childObject)
            {
                root[key] = value;

                return true;
            }

            current = childObject;
        }

        if (current.ContainsKey(parts[parts.Length - 1]))
            return false;

        current[parts[parts.Length - 1]] = value;

        return true;
    }
}
