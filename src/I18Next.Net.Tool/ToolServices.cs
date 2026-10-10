using System;
using System.Net.Http;

namespace I18Next.Net.Tool;

/// <summary>
///     Provides the environment the commands run in, so tests can replace it.
/// </summary>
internal class ToolServices
{
    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient());

    public virtual HttpClient HttpClient => SharedHttpClient.Value;

    public virtual string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
