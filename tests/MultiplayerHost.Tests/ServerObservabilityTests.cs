namespace MultiplayerHost.Tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;
using Xunit;

public class ServerObservabilityTests
{
    [Fact]
    public async Task StartAndStopAsync_EmitStructuredLifecycleLogs()
    {
        var logger = new CapturingLogger<Server>();
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser { Id = 1, LastSaved = DateTime.UtcNow });
        var connectionManager = new FakeConnectionManager();
        var turnProcessor = new TrackingTurnProcessor();
        var server = CreateConfiguredServer(logger, repository, connectionManager, turnProcessor, 10);

        await server.Start();
        await server.StopAsync();

        Assert.Contains(logger.Entries, entry => entry.EventId.Id == 1000 && entry.Message.Contains("Starting server.", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.EventId.Id == 1001 && entry.Message.Contains("Server stopped.", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Scope.ContainsKey("LifecycleOperation") && Equals(entry.Scope["LifecycleOperation"], "Start"));
        Assert.Contains(logger.Entries, entry => entry.Scope.ContainsKey("LifecycleOperation") && Equals(entry.Scope["LifecycleOperation"], "Stop"));
    }

    [Fact]
    public async Task RuntimeActivity_EmitsPersistenceAndDispatcherDiagnosticsLogs()
    {
        var logger = new CapturingLogger<Server>();
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser { Id = 7, IsDirty = true, LastSaved = DateTime.UtcNow.AddSeconds(-10) });
        var connectionManager = new FakeConnectionManager();
        var turnProcessor = new TrackingTurnProcessor();
        var server = CreateConfiguredServer(logger, repository, connectionManager, turnProcessor, 10);

        await server.Start();
        server.CreateServerMessage(5, 7, Messages.TargetKind.TargetList, "payload");
        await WaitForConditionAsync(() => logger.Entries.Any(entry => entry.EventId.Id == 1002));
        await server.StopAsync();

        Assert.Contains(logger.Entries, entry => entry.EventId.Id == 1002 && entry.Message.Contains("Persisted user 7", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.EventId.Id == 1200 && entry.Message.Contains("Dispatcher wake-up.", StringComparison.Ordinal));
    }

    private static Server CreateConfiguredServer(ILogger<Server> logger, FakeRepository repository, FakeConnectionManager connectionManager, TrackingTurnProcessor turnProcessor, int turnTimeMillis)
    {
        var server = new Server(logger);
        server.Context.Configure(repository, connectionManager, turnProcessor, turnTimeMillis);
        return server;
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(1);
        while (DateTime.UtcNow < timeoutAt)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Timed out waiting for the expected condition.");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> entries = [];
        private readonly Stack<IDictionary<string, object?>> scopes = new();

        public IReadOnlyList<LogEntry> Entries => entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            var scope = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    scope[pair.Key] = pair.Value;
                }
            }

            scopes.Push(scope);
            return new ScopeHandle(scopes);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Add(new LogEntry(
                logLevel,
                eventId,
                formatter(state, exception),
                scopes.Count > 0 ? new Dictionary<string, object?>(scopes.Peek()) : new Dictionary<string, object?>()));
        }

        public sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, IReadOnlyDictionary<string, object?> Scope);

        private sealed class ScopeHandle(Stack<IDictionary<string, object?>> scopes) : IDisposable
        {
            public void Dispose()
            {
                if (scopes.Count > 0)
                {
                    scopes.Pop();
                }
            }
        }
    }

    private sealed class FakeRepository : IRepository
    {
        public List<User> UsersToReturn { get; } = [];
        public ConcurrentBag<User> SavedUsers { get; } = [];

        public Task<IEnumerable<User>> GetUsers() => Task.FromResult<IEnumerable<User>>(UsersToReturn.ToArray());

        public Task<User?> GetUserAsync(int userId) => Task.FromResult(UsersToReturn.FirstOrDefault(user => user.Id == userId));

        public Task SaveUserAsync(User user)
        {
            SavedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(User user) => Task.CompletedTask;
    }

    private sealed class FakeConnectionManager : IConnectionManager
    {
        public event PlayerConnectingEventHandler? PlayerConnecting
        {
            add { }
            remove { }
        }

        public event PlayerDisconnectedEventHandler? PlayerDisconnected
        {
            add { }
            remove { }
        }

        public void DisconnectPlayer(int playerId)
        {
        }

        public Task SendMessage(in ServerMessage message) => Task.CompletedTask;
    }

    private sealed class TrackingTurnProcessor : ITurnProcessor
    {
        public Task OnTurnStart(uint tick, int elapsedMilliseconds) => Task.CompletedTask;

        public void ProcessClientMessage(User user, in ClientMessage msg)
        {
        }

        public Task ProcessUserTurn(User user, int elapsedMilliseconds) => Task.CompletedTask;

        public Task OnTurnComplete() => Task.CompletedTask;
    }

    private sealed class TestUser : User
    {
    }
}
