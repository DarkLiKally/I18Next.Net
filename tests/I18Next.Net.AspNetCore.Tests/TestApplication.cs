using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace I18Next.Net.AspNetCore.Tests;

internal static class TestApplication
{
    public static async Task<IHost> StartAsync(Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configure)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    configureServices(services);
                })
                .Configure(configure))
            .Build();

        await host.StartAsync();

        return host;
    }
}
