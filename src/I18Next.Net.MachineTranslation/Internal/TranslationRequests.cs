using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.MachineTranslation.Internal;

internal static class TranslationRequests
{
    public static IEnumerable<(int Start, int Count)> Split(IReadOnlyList<string> texts, int maxCount, int maxLength)
    {
        var start = 0;
        var count = 0;
        var length = 0;

        for (var i = 0; i < texts.Count; i++)
        {
            if (count > 0 && (count == maxCount || length + texts[i].Length > maxLength))
            {
                yield return (start, count);

                start = i;
                count = 0;
                length = 0;
            }

            count++;
            length += texts[i].Length;
        }

        if (count > 0)
            yield return (start, count);
    }

    public static HttpContent CreateJsonContent(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
            write(writer);

        var content = new ByteArrayContent(stream.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        return content;
    }

    public static async Task<JsonDocument> SendAsync(HttpClient httpClient, HttpRequestMessage request, string service, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var body = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new MachineTranslationException(
                $"{service} returned {(int)response.StatusCode} ({response.ReasonPhrase}): {GetErrorMessage(body)}", response.StatusCode);
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException e)
        {
            throw new MachineTranslationException($"{service} returned an invalid response.", e);
        }
    }

    public static MachineTranslationException UnexpectedResponse(string service)
    {
        return new MachineTranslationException($"{service} returned an unexpected response.");
    }

    private static string GetErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error))
                root = error;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString();
        }
        catch (JsonException)
        {
        }

        return body.Length > 500 ? body.Substring(0, 500) : body;
    }
}
