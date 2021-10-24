namespace MultiplayerHost.Domain
{
    using MultiplayerHost.Abstract;

    /// <summary>
    /// Provides a context for game logic execution.
    /// </summary>
    public class ServerContext
    {
        private const int MinTurnTime = 10;
        private const int MaxTurnTime = 60000;

        internal ServerContext(IServer server)
        {
            Server = server;
        }

        /// <summary>
        /// Provides the context with the game logic components.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="connectionManager"></param>
        /// <param name="turnProcessor"></param>
        /// <param name="turnTimeMillis">Milliseconds per server turn</param>
        public void Configure(IRepository repository, IConnectionManager connectionManager, ITurnProcessor turnProcessor, int turnTimeMillis)
        {
            Repository = repository;
            ConnectionManager = connectionManager;
            TurnProcessor = turnProcessor;

            if (turnTimeMillis < 10 || turnTimeMillis > MaxTurnTime) throw new System.ArgumentOutOfRangeException(nameof(turnTimeMillis), $"must be {MinTurnTime} - {MaxTurnTime}");
            TurnTimeMillis = turnTimeMillis;
        }

        /// <summary>
        /// Milliseconds per server turn.
        /// </summary>
        public int TurnTimeMillis { get; private set; }

        /// <summary>
        /// Returns the configured repository.
        /// </summary>
        public IRepository Repository { get; private set; }

        /// <summary>
        /// Returns the configured connection manager.
        /// </summary>
        public IConnectionManager ConnectionManager { get; private set; }

        /// <summary>
        /// Returns the configured turn processor.
        /// </summary>
        public ITurnProcessor TurnProcessor { get; private set; }

        /// <summary>
        /// Returns the server instance.
        /// </summary>
        public IServer Server { get; init; }

        /// <summary>
        /// Returns true if the context is correctly configured otherwise false.
        /// </summary>
        public bool IsContextValid =>
            Repository != null &&
            ConnectionManager != null &&
            TurnProcessor != null &&
            Server != null;
    }
}
