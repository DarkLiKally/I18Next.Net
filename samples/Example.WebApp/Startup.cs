using System;
using I18Next.Net.AspNetCore;
using I18Next.Net.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            // Enable request localization in order to determine the users desired language based on the Accept-Language header.
            app.UseRequestLocalization(options => options.AddSupportedCultures("de", "en"));

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Option 1: Simple setup for AspNetCore using the default configuration
            services.AddI18NextLocalization(i18N => i18N.IntegrateToAspNetCore());

            // Option 2: Customize the locales location in order to use the same json files on the client side. 
            // services.AddI18NextLocalization(i18n =>
            // {
            //     i18n.IntegrateToAspNetCore()
            //         .AddBackend(new JsonFileBackend("wwwroot/locales"));
            // });

            services.AddMvc()
                // Enable view localization and register required I18Next services
                .AddI18NextViewLocalization();
        }
    }
}
