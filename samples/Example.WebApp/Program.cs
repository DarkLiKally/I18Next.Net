using System.Globalization;
using System.Threading;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Example.WebApp;

public class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }

    public static void Main(string[] args)
    {
        // This is usually the case for production servers 
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CreateHostBuilder(args).Build().Run();
    }
}
