using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace I18Next.Net.Tool.Tests;

/// <summary>
///     A temporary directory to run the tool commands in.
/// </summary>
public sealed class ToolTestContext : IDisposable
{
    private readonly TestToolServices _services = new();

    public ToolTestContext()
    {
        Directory = Path.Combine(Path.GetTempPath(), "i18next-tool-tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
    }

    public string Directory { get; }

    public string Locales => GetPath("locales");

    public string Output { get; private set; }

    public string Error { get; private set; }

    public Dictionary<string, string> EnvironmentVariables => _services.Variables;

    public HttpMessageHandler HttpMessageHandler
    {
        set => _services.Client = new HttpClient(value);
    }

    public string GetPath(string relativePath)
    {
        return Path.Combine(Directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public string Write(string relativePath, string content)
    {
        var path = GetPath(relativePath);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));

        return path;
    }

    public string Read(string relativePath)
    {
        return File.ReadAllText(GetPath(relativePath));
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(GetPath(relativePath));
    }

    public async Task<int> RunAsync(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await ToolCommand.InvokeAsync(args, _services, output, error);

        Output = output.ToString().Replace(Directory + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
        Error = error.ToString();

        return exitCode;
    }

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class TestToolServices : ToolServices
    {
        public HttpClient Client { get; set; } = new(new FailingHandler());

        public Dictionary<string, string> Variables { get; } = [];

        public override HttpClient HttpClient => Client;

        public override string GetEnvironmentVariable(string name)
        {
            return Variables.GetValueOrDefault(name);
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Tests must not send real requests.");
        }
    }
}
