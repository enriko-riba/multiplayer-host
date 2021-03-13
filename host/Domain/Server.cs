namespace MultiplayerHost.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using MultiplayerHost.Abstract;
    using MultiplayerHost.Messages;

    /// <summary>
    /// Main game server instance.
    /// </summary>
    public partial class Server : IServer
    {
        private const int TICK_DURATION = 150;  //  TODO: make configurable

        private readonly RequestBuffer requestBuffer = new();
        private readonly ResponseBuffer responseBuffer = new();
        private readonly ServerContext context;
        private readonly ILogger logger;

        private Task dispatcherTask;
        private Task mainLoopTask;
        private ulong tickCounter;

        private readonly Dictionary<int, User> users = new();

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
        /// <summary>
        /// Raised after the users are loaded and before the main loop has started.
        /// </summary>
        public event EventHandler OnBeforeServerStart;

        /// <summary>
        /// Returns the server context.
        /// </summary>
        public ServerContext Context => context;

        /// <summary>
        /// Returns true if the server is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public async Task Start()
        {
            logger.LogInformation("starting server...");

            if (IsRunning) throw new Exception("Already running");
            if (!context.IsContextValid) throw new Exception("ServerContext is not valid");

            context.ConnectionManager.PlayerConnecting += ConnectionManager_PlayerConnected;
            context.ConnectionManager.PlayerDisconnected += ConnectionManager_PlayerDisconnected;

            //  load all users
            var activeUsers = await context.Repository.GetUsers();
            foreach (var user in activeUsers)
            {
                users.Add(user.Id, user);
            }
            logger.LogInformation("loaded {UserCount} users", activeUsers.Count());

            //  signal server start
            var handler = OnBeforeServerStart;
            handler?.Invoke(this, EventArgs.Empty);

            //  start background processes
            IsRunning = true;
            mainLoopTask = Task.Run(MainLoop);
            dispatcherTask = Task.Run(DispatcherLoop);
            await Task.Delay(50);
            logger.LogInformation("IRepository implementation: {IRepository}", context.Repository.GetType().FullName);
            logger.LogInformation("IConnectionManager implementation: {IConnectionManager}", context.ConnectionManager.GetType().FullName);
            logger.LogInformation("ITurnProcessor implementation: {ITurnProcessor}", context.TurnProcessor.GetType().FullName);
            logger.LogInformation("multiplayer host server is up & running!");
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            logger.LogWarning("stopping server...");
            IsRunning = false;
            mainLoopTask?.Wait();
            dispatcherTask?.Wait();
            logger.LogWarning("server stopped!");
        }

        /// <summary>
        /// Adds a new user to the game.
        /// </summary>
        /// <param name="user"></param>
        public void AddUser(User user)
        {
            logger.LogInformation("{@User}", user);
            users.Add(user.Id, user);
        }

        /// <summary>
        /// Removes the user from the game.
        /// </summary>
        /// <param name="user"></param>
        public void RemoveUser(User user)
        {
            logger.LogWarning("{@User}", user);
            if (users.Remove(user.Id))
            {
                context.Repository.DeleteUserAsync(user);
            }
        }

        /// <summary>
        /// Helper function to create and enqueue a server 2 client message.
        /// </summary>
        /// <param name="opCode">Game logic specific code.</param>
        /// <param name="target"></param>
        /// <param name="targetKind"></param>
        /// <param name="payload"></param>
        public void CreateServerMessage(int opCode, int target, TargetKind targetKind, string payload)
        {
            CreateServerMessage(opCode, new int[] { target }, targetKind, payload);
        }

        /// <summary>
        /// Helper function to create and enqueue a server 2 client message.
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="targets"></param>
        /// <param name="targetKind"></param>
        /// <param name="payload"></param>
        public void CreateServerMessage(int opCode, int[] targets, TargetKind targetKind, string payload)
        {
            var msg = new ServerMessage()
            {
                Created = DateTime.Now.Ticks,
                Tick = tickCounter,
                Code = opCode,
                Data = payload,
                TargetKind = targetKind,
                Targets = targets
            };
            responseBuffer.Write(in msg);
        }

        /// <summary>
        /// Enqueues the client message for server side processing.
        /// </summary>
        /// <param name="message"></param>
        public void EnqueueClientMessage(in ClientMessage message)
        {
            requestBuffer.Write(in message);
        }

        /// <summary>
        /// Returns the user collection.
        /// </summary>
        public IEnumerable<User> Users => this.users.Values;

        /// <summary>
        /// Returns the filtered user collection.
        /// </summary>
        public IEnumerable<User> FilterUsers(Predicate<User> p) => this.users.Values.Where(u => p(u));

        /// <summary>
        /// Gets the user by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public User GetUserById(int id)
        {
            users.TryGetValue(id, out var user);
            return user;
        }
        #endregion

        /// <summary>
        /// If true the user will be saved to repository.
        /// Base implementation returns true if the user state is dirty and at least one minute since last save has passed.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private bool ShouldSaveUser(User user)
        {
            return user.IsDirty && user.LastSaved.AddSeconds(5) < DateTime.UtcNow;
        }
        
        private void ConnectionManager_PlayerConnected(object sender, PlayerConnectingArgs pc)
        {
            if (users.TryGetValue(pc.PlayerId, out var user))
            {
                user.IsOnlineSince = DateTime.UtcNow;
                logger.LogInformation("{PlayerId}", pc.PlayerId);
            }
            else
            {
                pc.Cancel = true;
                logger.LogWarning("canceling connection for unknown player {PlayerId}", pc.PlayerId);
            }
        }
        
        private void ConnectionManager_PlayerDisconnected(object sender, PlayerDisconnectedArgs pd)
        {
            if (users.TryGetValue(pd.PlayerId, out var user))
            {
                user.IsOnlineSince = null;
                logger.LogInformation("{PlayerId}", pd.PlayerId);
            }
            else
            {
                logger.LogWarning("{PlayerId}", pd.PlayerId);
            }
        }
    }
}
