namespace MultiplayerHost.Domain;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiplayerHost.Abstract;
using MultiplayerHost.Messages;

/// <summary>
/// Main game server instance.
/// </summary>
public partial class Server : IServer
{
    private static readonly TimeSpan UserSaveInterval = TimeSpan.FromSeconds(5);
    private static readonly Meter DiagnosticsMeter = new("MultiplayerHost.Server");
    private static readonly Counter<int> ReceivedClientMessagesCounter = DiagnosticsMeter.CreateCounter<int>("multiplayerhost.server.client_messages_processed");
    private static readonly Counter<int> DispatchedServerMessagesCounter = DiagnosticsMeter.CreateCounter<int>("multiplayerhost.server.server_messages_dispatched");
    private static readonly Counter<int> PersistedUsersCounter = DiagnosticsMeter.CreateCounter<int>("multiplayerhost.server.users_persisted");

    private static readonly EventId StartupEventId = new(1000, nameof(StartupEventId));
    private static readonly EventId ShutdownEventId = new(1001, nameof(ShutdownEventId));
    private static readonly EventId UserPersistenceEventId = new(1002, nameof(UserPersistenceEventId));
    private static readonly EventId UserConnectionEventId = new(1003, nameof(UserConnectionEventId));
    private static readonly EventId UserDisconnectionEventId = new(1004, nameof(UserDisconnectionEventId));

    private readonly RequestBuffer requestBuffer = new();
    private readonly ResponseBuffer responseBuffer = new();
    private readonly ServerContext context;
    private readonly ILogger logger;
    private readonly SemaphoreSlim lifecycleLock = new(1, 1);

    private Task? dispatcherTask;
    private Task? mainLoopTask;
    private CancellationTokenSource? shutdownTokenSource;
    private int tickCounter;

    private readonly ConcurrentDictionary<int, User> users = [];

    /// <summary>
    /// Creates a new Server instance.
    /// </summary>
    /// <param name="logger"></param>
    public Server(ILogger<Server> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        context = new ServerContext(this);
    }

    #region IServer implementation
    /// <inheritdoc />
    public event EventHandler? OnBeforeServerStart;

    /// <inheritdoc />    
    public event AsyncEventHandler<EventArgs>? OnBeforeServerStartAsync;
    
    /// <inheritdoc />
    public ServerContext Context => context;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public async Task Start()
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ServerComponent"] = nameof(Server),
            ["LifecycleOperation"] = "Start"
        });

        logger.LogInformation(StartupEventId, "Starting server.");

        await lifecycleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("The server is already running.");
            }

            if (!context.IsContextValid)
            {
                throw new InvalidOperationException("The server context is not configured.");
            }

            shutdownTokenSource?.Dispose();
            shutdownTokenSource = new CancellationTokenSource();
            var cancellationToken = shutdownTokenSource.Token;

            context.ConnectionManager.PlayerConnecting += ConnectionManager_PlayerConnecting;
            context.ConnectionManager.PlayerDisconnected += ConnectionManager_PlayerDisconnected;

            users.Clear();
            Interlocked.Exchange(ref tickCounter, 0);

            try
            {
                var activeUsers = await context.Repository.GetUsers().ConfigureAwait(false);
                foreach (var user in activeUsers)
                {
                    users[user.Id] = user;
                }

                logger.LogInformation(StartupEventId, "Loaded {UserCount} users.", users.Count);

                var handler = OnBeforeServerStart;
                handler?.Invoke(this, EventArgs.Empty);
                await InvokeAsync(OnBeforeServerStartAsync, this, EventArgs.Empty).ConfigureAwait(false);

                IsRunning = true;
                mainLoopTask = Task.Run(() => MainLoop(cancellationToken), cancellationToken);
                dispatcherTask = Task.Run(() => DispatcherLoop(cancellationToken), cancellationToken);
            }
            catch
            {
                context.ConnectionManager.PlayerConnecting -= ConnectionManager_PlayerConnecting;
                context.ConnectionManager.PlayerDisconnected -= ConnectionManager_PlayerDisconnected;
                users.Clear();
                IsRunning = false;
                shutdownTokenSource.Dispose();
                shutdownTokenSource = null;
                mainLoopTask = null;
                dispatcherTask = null;
                throw;
            }
        }
        finally
        {
            lifecycleLock.Release();
        }

        logger.LogInformation(StartupEventId, "IRepository implementation: {IRepository}", context.Repository.GetType().FullName);
        logger.LogInformation(StartupEventId, "IConnectionManager implementation: {IConnectionManager}", context.ConnectionManager.GetType().FullName);
        logger.LogInformation(StartupEventId, "ITurnProcessor implementation: {ITurnProcessor}", context.TurnProcessor.GetType().FullName);
        logger.LogInformation(StartupEventId, "Multiplayer host server is up and running.");
    }

    /// <inheritdoc />
    public void Stop()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ServerComponent"] = nameof(Server),
            ["LifecycleOperation"] = "Stop"
        });

        logger.LogWarning(ShutdownEventId, "Stopping server.");

        Task? mainLoop;
        Task? dispatcherLoop;
        CancellationTokenSource? tokenSource;

        await lifecycleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("The server is not running.");
            }

            IsRunning = false;
            tokenSource = shutdownTokenSource;
            mainLoop = mainLoopTask;
            dispatcherLoop = dispatcherTask;

            shutdownTokenSource = null;
            mainLoopTask = null;
            dispatcherTask = null;

            context.ConnectionManager.PlayerConnecting -= ConnectionManager_PlayerConnecting;
            context.ConnectionManager.PlayerDisconnected -= ConnectionManager_PlayerDisconnected;

            tokenSource?.Cancel();
        }
        finally
        {
            lifecycleLock.Release();
        }

        try
        {
            await AwaitBackgroundTasks(mainLoop, dispatcherLoop).ConfigureAwait(false);
        }
        finally
        {
            tokenSource?.Dispose();
        }

        logger.LogWarning(ShutdownEventId, "Server stopped. ActiveUsers={ActiveUsers}, PendingResponses={PendingResponses}", users.Count, responseBuffer.Count);
    }

    /// <inheritdoc />
    public void AddUser(User user)
    {
        logger.LogInformation("{@User}", user);
        if (!users.TryAdd(user.Id, user))
        {
            throw new InvalidOperationException($"A user with id {user.Id} already exists.");
        }
    }

    /// <inheritdoc />
    public void RemoveUser(User user)
    {
        logger.LogWarning("{@User}", user);
        if (users.TryRemove(user.Id, out _))
        {
            _ = context.Repository.DeleteUserAsync(user);
        }
    }

    /// <inheritdoc />
    public void CreateServerMessage(int opCode, int target, TargetKind targetKind, string payload)
    {
        CreateServerMessage(opCode, [target], targetKind, payload);
    }

    /// <inheritdoc />
    public void CreateServerMessage(int opCode, int[] targets, TargetKind targetKind, string payload)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(payload);

        var msg = new ServerMessage(
            (ulong)Volatile.Read(ref tickCounter),
            DateTime.UtcNow.Ticks,
            opCode,
            payload,
            targetKind,
            targets);

        responseBuffer.Write(in msg);
    }

    /// <inheritdoc />
    public void EnqueueClientMessage(in ClientMessage message)
    {
        requestBuffer.Write(in message);
    }

    /// <inheritdoc />
    public IEnumerable<User> Users => this.users.Values;

    /// <inheritdoc />
    public IEnumerable<User> FilterUsers(Predicate<User> p) => this.users.Values.Where(u => p(u));

    /// <inheritdoc />
    public User? GetUserById(int id)
    {
        users.TryGetValue(id, out var user);
        return user;
    }
    #endregion
      
    /// <summary>
    /// Helper to await all subscribers.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private static Task InvokeAsync(AsyncEventHandler<EventArgs>? evt, object? sender, EventArgs e) =>
        evt is null
            ? Task.CompletedTask
            : Task.WhenAll(evt.GetInvocationList()
                              .Cast<AsyncEventHandler<EventArgs>>()
                              .Select(h => h(sender, e)));

    private static async Task AwaitBackgroundTasks(params Task?[] tasks)
    {
        var runningTasks = tasks.Where(task => task is not null).Cast<Task>().ToArray();
        if (runningTasks.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(runningTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during coordinated shutdown when background loops observe cancellation.
        }
    }

    internal int UserCount => users.Count;


    /// <summary>
    /// Returns true when the user is dirty and the save interval has elapsed.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private static bool ShouldSaveUser(User user)
    {
        return user.IsDirty && user.LastSaved + UserSaveInterval < DateTime.UtcNow;
    }

    /// <summary>
    /// Handler for the <see cref="IConnectionManager.PlayerConnecting"/> event.
    /// Setups the user online state and logs the connection.
    /// Note: if the user is not found in the 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="pc"></param>
    private void ConnectionManager_PlayerConnecting(object? sender, PlayerConnectingArgs pc)
    {
        if (users.TryGetValue(pc.PlayerId, out var user))
        {
            user.IsOnlineSince = DateTime.UtcNow;
            logger.LogInformation(UserConnectionEventId, "Accepted connection for player {PlayerId}", pc.PlayerId);
        }
        else
        {
            pc.Cancel = true;
            logger.LogWarning(UserConnectionEventId, "Canceling connection for unknown player {PlayerId}", pc.PlayerId);
        }
    }
    
    private void ConnectionManager_PlayerDisconnected(object? sender, PlayerDisconnectedArgs pd)
    {
        if (users.TryGetValue(pd.PlayerId, out var user))
        {
            user.IsOnlineSince = null;
            logger.LogInformation(UserDisconnectionEventId, "Player {PlayerId} disconnected.", pd.PlayerId);
        }
        else
        {
            logger.LogWarning(UserDisconnectionEventId, "Received disconnect event for unknown player {PlayerId}", pd.PlayerId);
        }
    }

    private static void RecordClientMessageProcessed() => ReceivedClientMessagesCounter.Add(1);

    private static void RecordServerMessageDispatched() => DispatchedServerMessagesCounter.Add(1);

    private void RecordUserPersisted(User user)
    {
        PersistedUsersCounter.Add(1);
        logger.LogDebug(UserPersistenceEventId, "Persisted user {PlayerId} after the save interval elapsed.", user.Id);
    }
}
