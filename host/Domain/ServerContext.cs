namespace MultiplayerHost.Domain;

using System;
using MultiplayerHost.Abstract;

/// <summary>
/// Provides a context for game logic execution.
/// </summary>
public class ServerContext
{
    private const int MinTurnTime = 10;
    private const int MaxTurnTime = 60000;

    private IRepository? repository;
    private IConnectionManager? connectionManager;
    private ITurnProcessor? turnProcessor;

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
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(turnProcessor);

        this.repository = repository;
        this.connectionManager = connectionManager;
        this.turnProcessor = turnProcessor;

        if (turnTimeMillis < MinTurnTime || turnTimeMillis > MaxTurnTime)
        {
            throw new ArgumentOutOfRangeException(nameof(turnTimeMillis), $"must be {MinTurnTime} - {MaxTurnTime}");
        }

        TurnTimeMillis = turnTimeMillis;
    }

    /// <summary>
    /// Milliseconds per server turn.
    /// </summary>
    public int TurnTimeMillis { get; private set; }

    /// <summary>
    /// Returns the configured repository.
    /// </summary>
    public IRepository Repository => repository ?? throw new InvalidOperationException("The server context repository has not been configured.");

    /// <summary>
    /// Returns the configured connection manager.
    /// </summary>
    public IConnectionManager ConnectionManager => connectionManager ?? throw new InvalidOperationException("The server context connection manager has not been configured.");

    /// <summary>
    /// Returns the configured turn processor.
    /// </summary>
    public ITurnProcessor TurnProcessor => turnProcessor ?? throw new InvalidOperationException("The server context turn processor has not been configured.");

    /// <summary>
    /// Returns the server instance.
    /// </summary>
    public IServer Server { get; init; }

    /// <summary>
    /// Returns true if the context is correctly configured otherwise false.
    /// </summary>
    public bool IsContextValid =>
        repository != null &&
        connectionManager != null &&
        turnProcessor != null &&
        Server != null;
}
