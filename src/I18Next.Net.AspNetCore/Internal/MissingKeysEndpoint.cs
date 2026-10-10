#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Plugins;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.AspNetCore.Internal;

internal sealed class MissingKeysEndpoint(I18NextMissingKeysOptions options)
{
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 16 };

    private readonly ResourceNameFilter _filter = new(options);
    private readonly int _maxKeys = options.MaxKeys;
    private readonly long _maxRequestBodySize = options.MaxRequestBodySize;

    public async Task HandleAsync(HttpContext context)
    {
        var language = _filter.GetLanguage(context.Request.RouteValues["lng"] as string);
        var ns = _filter.GetNamespace(context.Request.RouteValues["ns"] as string);

        if (language == null || ns == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        if (!context.Request.HasJsonContentType())
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

            return;
        }

        if (context.Request.ContentLength > _maxRequestBodySize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

            return;
        }

        var body = await ReadBodyAsync(context.Request);

        if (body == null)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

            return;
        }

        var keys = ReadKeys(body);

        if (keys == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return;
        }

        var handlers = context.RequestServices.GetServices<IMissingKeyHandler>();

        foreach (var key in keys)
        {
            var args = new MissingKeyEventArgs(language, ns, key, [key]);

            foreach (var handler in handlers)
                await handler.HandleMissingKeyAsync(context, args);
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private async Task<byte[]> ReadBodyAsync(HttpRequest request)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[4096];
        int read;

        while ((read = await request.Body.ReadAsync(buffer, request.HttpContext.RequestAborted)) > 0)
        {
            if (stream.Length + read > _maxRequestBodySize)
                return null;

            stream.Write(buffer, 0, read);
        }

        return stream.ToArray();
    }

    private List<string> ReadKeys(byte[] body)
    {
        try
        {
            using var document = JsonDocument.Parse(body, DocumentOptions);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var keys = new List<string>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                    return null;

                if (seenKeys.Add(property.Name))
                    keys.Add(property.Name);

                if (keys.Count > _maxKeys)
                    return null;
            }

            return keys;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
#endif
