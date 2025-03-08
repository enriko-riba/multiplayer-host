using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MultiplayerHost.Abstract;

namespace MultiplayerHost.ReferenceGame;

/// <summary>
/// Hosted service controlled by the host. 
/// Glues together all game dependencies and starts the game server.
/// </summary>
public class Application(IServer server,
    IRepository repository,
    IConnectionManager connectionManager,
    ITurnProcessor turnProcessor) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var context = server.Context;
        context.Configure(repository, connectionManager, turnProcessor, 100);

        //  after the server is started the following happens:
        //  1. The main loop and dispatcher are started on background threads
        //  2. The OnBeforeServerStart event is fired
        //  3. Main loop starts, repeatedly invoking ITurnProcessor methods
        //      - OnTurnStart(),
        //      - ProcessClientMessage() for each client message received, 
        //      - ProcessUserTurn() for each user in repository
        //      - OnTurnComplete()
        await server.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        server.Stop();
        return Task.CompletedTask;
    }
}
