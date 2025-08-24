namespace MultiplayerHost.Abstract;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;

/// <summary>
/// delegate for async event handlers.
/// </summary>
/// <typeparam name="TEventArgs"></typeparam>
/// <param name="sender"></param>
/// <param name="e"></param>
/// <returns></returns>
public delegate Task AsyncEventHandler<in TEventArgs>(object? sender, TEventArgs e);

/// <summary>
/// Supports server operations. 
/// </summary>
public interface IServer
{
    /// <summary>
    /// Raised after the server is initialized and all users loaded but before the main processing loop has started.
    /// </summary>
    event EventHandler OnBeforeServerStart;

    /// <summary>
    /// Raised after the server is initialized and all users loaded but before the main processing loop has started.
    /// </summary>
    event AsyncEventHandler<EventArgs>? OnBeforeServerStartAsync;

    /// <summary>
    /// Starts the server main processing loop and message dispatcher.
    /// Note: throws if the <see cref="ServerContext"/> is not configured.
    /// </summary>
    /// <returns></returns>
    Task Start();

    /// <summary>
    /// Stops the server main processing loop and message dispatcher.
    /// </summary>
    /// <returns></returns>
    void Stop();

    /// <summary>
    /// Returns true if the main processing loop and message dispatcher have been started.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Adds the user instance to the server.
    /// </summary>
    /// <param name="user"></param>
    void AddUser(User user);

    /// <summary>
    /// Removes the user instance.
    /// </summary>
    /// <param name="user"></param>
    void RemoveUser(User user);

    /// <summary>
    /// Returns all users.
    /// </summary>
    IEnumerable<User> Users { get; }

    /// <summary>
    /// Filters users by the predicate.
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    IEnumerable<User> FilterUsers(Predicate<User> filterExpression);

    /// <summary>
    /// Gets a user by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    User? GetUserById(int id);

    /// <summary>
    /// Stores an incoming client message to the processing queue.
    /// </summary>
    /// <param name="message"></param>
    void EnqueueClientMessage(in ClientMessage message);

    /// <summary>
    /// Creates a server message and sends it to clients based on the <see cref="TargetKind"/> and targets combination.
    /// </summary>
    /// <param name="opCode">Game specific code.</param>
    /// <param name="targets">user IDs to which the message gets dispatched.</param>
    /// <param name="targetKind"></param>
    /// <param name="payload">Game and opCode specific payload.</param>
    void CreateServerMessage(int opCode, int[] targets, TargetKind targetKind, string payload);

    /// <summary>
    /// Creates a server message and sends it to clients based on the <see cref="TargetKind"/> and target combination.
    /// </summary>
    /// <param name="opCode">Game logic specific code.</param>
    /// <param name="target">user ID to whom the message gets dispatched.</param>
    /// <param name="targetKind"></param>
    /// <param name="payload">Game and opCode specific payload.</param>
    void CreateServerMessage(int opCode, int target, TargetKind targetKind, string payload);

    /// <summary>
    /// Returns the server context object.
    /// Note: before the server can be started the context must be configured and <see cref="ServerContext.IsContextValid"/> must return true.
    /// </summary>
    ServerContext Context { get; }
}
