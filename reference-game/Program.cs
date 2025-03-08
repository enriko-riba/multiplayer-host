
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;
using MultiplayerHost.ReferenceGame;


CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((builder) =>
    {
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss:fff ";
        });
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(hostContext.Configuration);
        services.AddSingleton<Application>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IRepository, DummyRepository>();
        services.AddSingleton<IServer, Server>();
        services.AddSingleton<ITurnProcessor, GameLogic>();
        services.AddHostedService<Application>();
    })
    .Build();

await host.RunAsync();
