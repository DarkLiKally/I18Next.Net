using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;
using I18Next.Net.Yaml;

Console.WriteLine("# YAML files");
await Show(new YamlFileBackend(Path.Combine(AppContext.BaseDirectory, "yaml")));

Console.WriteLine("# Embedded resources through a FuncBackend");
await Show(FuncBackend.FromStream((language, ns) => typeof(Program).Assembly.GetManifestResourceStream($"Locales.{language}.{ns}.json")));

Console.WriteLine("# Objects through a FuncBackend");
await Show(FuncBackend.FromObject((language, ns) => language == "de"
    ? new { greeting = "Hallo {{name}} aus einem Objekt!" }
    : new { greeting = "Hello {{name}} from an object!" }));

Console.WriteLine("# HTTP");
var httpClient = new HttpClient(new FileServer(Path.Combine(AppContext.BaseDirectory, "remote"))) { BaseAddress = new Uri("https://cdn.example.com/") };
var http = new HttpBackend(httpClient, "locales/{{lng}}/{{ns}}.json") { QueryStringParams = { ["v"] = "2.0.0" } };
await Show(http);

Console.WriteLine("# Chained: local overrides, then HTTP, cached for five minutes");
var overrides = new InMemoryBackend();
overrides.AddTranslation("en", "translation", "greeting", "Hello {{name}} from the local overrides!");

var chained = new ChainedBackend(overrides, http)
{
    CacheEnabled = true,
    CacheExpiration = TimeSpan.FromMinutes(5)
};
await Show(chained);

return;

static async Task Show(ITranslationBackend backend)
{
    var i18n = new I18NextNet(backend, new DefaultTranslator(backend));

    foreach (var language in new[] { "en", "de" })
        Console.WriteLine($"{language}: {await i18n.Ta(language, "greeting", new { name = "Jane" })}");

    Console.WriteLine();
}

internal class FileServer(string root) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, request.RequestUri!.AbsolutePath.Replace("/locales/", "").TrimStart('/'));

        Console.WriteLine($"  GET {request.RequestUri}");

        return Task.FromResult(File.Exists(path)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(File.ReadAllText(path)) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
