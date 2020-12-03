using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MultiplayerHost.Abstract;

namespace MultiplayerHost.ReferenceGame
{
    /// <summary>
    /// Hosted service controlled by the host. 
    /// Glues together all game dependencies and starts the game server.
    /// </summary>
    public class Application : IHostedService
    {
        private readonly IServer server;
        private readonly IRepository repository;
        private readonly IConnectionManager connMngr;
        private readonly ITurnProcessor turnProcessor;

        public Application(IServer server, IRepository repository, IConnectionManager connMngr, ITurnProcessor turnProcessor)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.connMngr = connMngr ?? throw new ArgumentNullException(nameof(connMngr));
            this.turnProcessor = turnProcessor ?? throw new ArgumentNullException(nameof(turnProcessor));
        }

        #region IHostedService implementation
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var context = server.Context;
            context.Configure(repository, connMngr, turnProcessor);

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
        #endregion
    }
}
