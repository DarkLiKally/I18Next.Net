using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.MachineTranslation.Tests;

public class FakeHttpMessageHandler(Func<CapturedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = [];

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
        var captured = new CapturedRequest(request.Method, request.RequestUri, headers, request.Content?.Headers.ContentType?.ToString(), body);

        lock (Requests)
            Requests.Add(captured);

        return respond(captured);
    }
}

public class CapturedRequest(HttpMethod method, Uri uri, Dictionary<string, string> headers, string contentType, string body)
{
    public string Body { get; } = body;

    public string ContentType { get; } = contentType;

    public Dictionary<string, string> Headers { get; } = headers;

    public HttpMethod Method { get; } = method;

    public Uri Uri { get; } = uri;

    public JsonElement Json => JsonDocument.Parse(Body).RootElement;
}
