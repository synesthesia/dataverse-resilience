using DVConsole.Configuration;
using DVConsole.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace DVConsole
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            try
            {
                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddHostedService<DataverseConsoleExample01>()
                        .Configure<DataVerseOptions>
                            (hostContext.Configuration.GetSection("Dataverse"))
                        //.UseDataVerse(ServiceCollectionExtensions.DataverseConnectionMode.UserLogin)
                        .UseDataVerse(ServiceCollectionExtensions.DataverseConnectionMode.ClientSecret)
                        ;
                })
                .UseSerilog()
                .UseConsoleLifetime()
            ;
    }
}