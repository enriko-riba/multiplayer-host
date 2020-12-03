using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;

namespace MultiplayerHost.ReferenceGame
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            await CreateHostBuilder(args).Build().RunAsync();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder()
                .ConfigureLogging((builder) =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss:fff ";
                    });
                })

                //.UseSerilog(CreateLogger())
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(hostContext.Configuration);
                    services.AddSingleton<Application>();
                    services.AddSingleton<IConnectionManager, ConnectionManager>();
                    services.AddSingleton<IRepository, DummyRepository>();
                    services.AddSingleton<IServer, Server>();
                    services.AddSingleton<ITurnProcessor, GameLogic>();
                    services.AddHostedService<Application>();
                });
    }
}
