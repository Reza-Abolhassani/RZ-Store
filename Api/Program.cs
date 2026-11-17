using Autofac.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Serilog;

using System;
using System.Threading.Tasks;

namespace MyApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())

                // ✅ Serilog as the logging provider
                .UseSerilog((ctx, services, cfg) =>
                    cfg.ReadFrom.Configuration(ctx.Configuration)
                       .ReadFrom.Services(services)
                       .Enrich.FromLogContext())

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // ✅ Sentry reads "Sentry" section from configuration by default

                    webBuilder.UseStartup<Startup>();
                });
    }
}
