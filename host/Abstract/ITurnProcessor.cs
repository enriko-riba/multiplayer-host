namespace MultiplayerHost.Abstract;

using System.Threading.Tasks;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;

/// <summary>
/// Defines turn oriented game logic.
/// </summary>
public interface ITurnProcessor
{
    /// <summary>
    /// Invoked by the server before a new turn starts.
    /// If no implementation is needed return <see cref="Task.CompletedTask"/>.
    /// </summary>
    /// <param name="tick">the server turn counter.</param>
    /// <param name="elapsedMilliseconds">elapsed time in milliseconds since last turn</param>
    /// <returns></returns>
    Task OnTurnStart(uint tick, int elapsedMilliseconds);

    /// <summary>
    /// Invoked by the server for every message received from clients.
    /// The implementation is expected to return immediately with processing reduced to bare minimum.
    /// For example processing of a 'MoveTo x,y' message should only update player state: player.Destination = (x,y);
    /// and return. The movement processing is calculated inside the <see cref="ProcessUserTurn(User, int)"/> method where the elapsed time is available.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="msg"></param>
    void ProcessClientMessage(User user, in ClientMessage msg);

    /// <summary>
    /// Main game logic processing method. Invoked by the server for each user (both connected and disconnected).
    /// Note: the server is every 5 seconds checking the <see cref="User.IsDirty"/> and persisting player state via <see cref="IRepository.SaveUserAsync"/> method.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="elapsedMilliseconds">elapsed time in milliseconds since last turn</param>
    /// <returns></returns>
    Task ProcessUserTurn(User user, int elapsedMilliseconds);

    /// <summary>
    /// Invoked by the server after the turn processing has finished and before the next turn.
    /// If no implementation is needed return <see cref="Task.CompletedTask"/>.
    /// </summary>
    /// <returns></returns>
    Task OnTurnComplete();
}
