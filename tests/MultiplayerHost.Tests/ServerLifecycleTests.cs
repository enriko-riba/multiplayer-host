namespace MultiplayerHost.Tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;
using Xunit;

public class ServerLifecycleTests
{
    [Fact]
    public async Task StartAndStopAsync_ManageLifecycleAndSubscriptions()
    {
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser { Id = 42, LastSaved = DateTime.UtcNow });
        var connectionManager = new FakeConnectionManager();
        var turnProcessor = new TrackingTurnProcessor();
        var server = CreateConfiguredServer(repository, connectionManager, turnProcessor, 10);

        await server.Start();

        Assert.True(server.IsRunning);
        Assert.Single(server.Users);
        Assert.Equal(1, connectionManager.PlayerConnectingSubscriptionCount);
        Assert.Equal(1, connectionManager.PlayerDisconnectedSubscriptionCount);

        await server.StopAsync();

        Assert.False(server.IsRunning);
        Assert.Equal(0, connectionManager.PlayerConnectingSubscriptionCount);
        Assert.Equal(0, connectionManager.PlayerDisconnectedSubscriptionCount);
        Assert.True(turnProcessor.OnTurnStartCalls >= 1);
        Assert.True(turnProcessor.OnTurnCompleteCalls >= 1);
    }

    [Fact]
    public async Task Stop_WhenServerIsRunning_CompletesViaCompatibilityWrapper()
    {
        var server = CreateConfiguredServer(new FakeRepository(), new FakeConnectionManager(), new TrackingTurnProcessor(), 10);

        await server.Start();
        server.Stop();

        Assert.False(server.IsRunning);
    }

    [Fact]
    public async Task Start_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        var server = CreateConfiguredServer(new FakeRepository(), new FakeConnectionManager(), new TrackingTurnProcessor(), 10);

        await server.Start();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.Start());
        await server.StopAsync();

        Assert.Equal("The server is already running.", exception.Message);
    }

    [Fact]
    public async Task StopAsync_WhenServerIsNotRunning_ThrowsInvalidOperationException()
    {
        var server = CreateConfiguredServer(new FakeRepository(), new FakeConnectionManager(), new TrackingTurnProcessor(), 10);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.StopAsync());

        Assert.Equal("The server is not running.", exception.Message);
    }

    [Fact]
    public async Task Start_WhenContextIsNotConfigured_ThrowsInvalidOperationException()
    {
        var server = new Server(NullLogger<Server>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.Start());

        Assert.Equal("The server context is not configured.", exception.Message);
    }

    [Fact]
    public async Task StopAsync_DrainsQueuedMessagesBeforeShutdownCompletes()
    {
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser { Id = 7, LastSaved = DateTime.UtcNow });
        var connectionManager = new FakeConnectionManager();
        var server = CreateConfiguredServer(repository, connectionManager, new TrackingTurnProcessor(), 10);

        await server.Start();
        server.CreateServerMessage(1001, 7, TargetKind.TargetList, "payload");
        await WaitForConditionAsync(() => connectionManager.SentMessages.Any(msg => msg.Code == 1001 && msg.Data == "payload"));
        await server.StopAsync();

        Assert.Contains(connectionManager.SentMessages, msg => msg.Code == 1001 && msg.Data == "payload");
    }

    [Fact]
    public async Task ProcessUserTurn_PersistsDirtyUsersOnlyAfterSaveIntervalElapsed()
    {
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser
        {
            Id = 9,
            IsDirty = true,
            LastSaved = DateTime.UtcNow.AddSeconds(-10)
        });

        var server = CreateConfiguredServer(repository, new FakeConnectionManager(), new TrackingTurnProcessor(), 10);

        await server.Start();
        await WaitForConditionAsync(() => repository.SavedUsers.Any(user => user.Id == 9));
        await server.StopAsync();

        Assert.Contains(repository.SavedUsers, user => user.Id == 9);
    }

    [Fact]
    public async Task ProcessUserTurn_DoesNotPersistDirtyUsersBeforeSaveIntervalElapsed()
    {
        var repository = new FakeRepository();
        repository.UsersToReturn.Add(new TestUser
        {
            Id = 10,
            IsDirty = true,
            LastSaved = DateTime.UtcNow
        });

        var server = CreateConfiguredServer(repository, new FakeConnectionManager(), new TrackingTurnProcessor(), 10);

        await server.Start();
        await Task.Delay(50);
        await server.StopAsync();

        Assert.DoesNotContain(repository.SavedUsers, user => user.Id == 10);
    }

    [Fact]
    public void Configure_WhenAnyDependencyIsNull_ThrowsArgumentNullException()
    {
        var server = new Server(NullLogger<Server>.Instance);
        var repository = new FakeRepository();
        var connectionManager = new FakeConnectionManager();
        var turnProcessor = new TrackingTurnProcessor();

        Assert.Throws<ArgumentNullException>(() => server.Context.Configure(null!, connectionManager, turnProcessor, 10));
        Assert.Throws<ArgumentNullException>(() => server.Context.Configure(repository, null!, turnProcessor, 10));
        Assert.Throws<ArgumentNullException>(() => server.Context.Configure(repository, connectionManager, null!, 10));
    }

    [Fact]
    public void ClientMessageConstructor_WhenPayloadIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ClientMessage(1, 100, null!));
    }

    [Fact]
    public void ServerMessageConstructor_WhenPayloadOrTargetsAreNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ServerMessage(1, DateTime.UtcNow.Ticks, 42, null!, TargetKind.TargetList, []));
        Assert.Throws<ArgumentNullException>(() => new ServerMessage(1, DateTime.UtcNow.Ticks, 42, "payload", TargetKind.TargetList, null!));
    }

    [Fact]
    public void MessageConstructors_AssignSafeDefaultsAndProvidedValues()
    {
        var clientMessage = new ClientMessage(5, 99, "client-payload")
        {
            Cid = 12,
            Created = 1234
        };

        var serverMessage = new ServerMessage(3, 5678, 55, "server-payload", TargetKind.TargetList, [1, 2]);

        Assert.Equal(5, clientMessage.UserId);
        Assert.Equal(99, clientMessage.Code);
        Assert.Equal("client-payload", clientMessage.Data);
        Assert.Equal(12, clientMessage.Cid);
        Assert.Equal(1234, clientMessage.Created);

        Assert.Equal((ulong)3, serverMessage.Tick);
        Assert.Equal(5678, serverMessage.Created);
        Assert.Equal(55, serverMessage.Code);
        Assert.Equal("server-payload", serverMessage.Data);
        Assert.Equal(TargetKind.TargetList, serverMessage.TargetKind);
        Assert.Equal([1, 2], serverMessage.Targets);
    }

    private static Server CreateConfiguredServer(FakeRepository repository, FakeConnectionManager connectionManager, TrackingTurnProcessor turnProcessor, int turnTimeMillis)
    {
        var server = new Server(NullLogger<Server>.Instance);
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

    private sealed class FakeRepository : IRepository
    {
        public List<User> UsersToReturn { get; } = [];
        public ConcurrentBag<User> SavedUsers { get; } = [];
        public ConcurrentBag<User> DeletedUsers { get; } = [];

        public Task<IEnumerable<User>> GetUsers() => Task.FromResult<IEnumerable<User>>(UsersToReturn.ToArray());

        public Task<User?> GetUserAsync(int userId) => Task.FromResult(UsersToReturn.FirstOrDefault(user => user.Id == userId));

        public Task SaveUserAsync(User user)
        {
            SavedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(User user)
        {
            DeletedUsers.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeConnectionManager : IConnectionManager
    {
        private PlayerConnectingEventHandler? playerConnecting;
        private PlayerDisconnectedEventHandler? playerDisconnected;

        public event PlayerConnectingEventHandler? PlayerConnecting
        {
            add => playerConnecting += value;
            remove => playerConnecting -= value;
        }

        public event PlayerDisconnectedEventHandler? PlayerDisconnected
        {
            add => playerDisconnected += value;
            remove => playerDisconnected -= value;
        }

        public int PlayerConnectingSubscriptionCount => playerConnecting?.GetInvocationList().Length ?? 0;

        public int PlayerDisconnectedSubscriptionCount => playerDisconnected?.GetInvocationList().Length ?? 0;

        public ConcurrentBag<ServerMessage> SentMessages { get; } = [];

        public void DisconnectPlayer(int playerId)
        {
        }

        public Task SendMessage(in ServerMessage message)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingTurnProcessor : ITurnProcessor
    {
        public int OnTurnStartCalls { get; private set; }
        public int OnTurnCompleteCalls { get; private set; }
        public int ProcessUserTurnCalls { get; private set; }
        public int ProcessClientMessageCalls { get; private set; }

        public Task OnTurnStart(uint tick, int elapsedMilliseconds)
        {
            OnTurnStartCalls++;
            return Task.CompletedTask;
        }

        public void ProcessClientMessage(User user, in ClientMessage msg)
        {
            ProcessClientMessageCalls++;
        }

        public Task ProcessUserTurn(User user, int elapsedMilliseconds)
        {
            ProcessUserTurnCalls++;
            return Task.CompletedTask;
        }

        public Task OnTurnComplete()
        {
            OnTurnCompleteCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestUser : User
    {
    }
}
