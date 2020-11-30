namespace MultiplayerHost.Abstract
{
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
        /// <returns></returns>
        Task OnTurnStart(long tick);

        /// <summary>
        /// Invoked by the server for every message received from clients.
        /// The implementation is expected to return immediately with processing reduced to bare minimum.
        /// For example proccessing of a 'MoveTo x,y' message should only update player state: player.Destination = (x,y);
        /// and return. The movement processing is calulated inside the <see cref="ProcessUserTurn(User, int)"/> method where the ellapsed time is available.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="msg"></param>
        void ProcessClientMessage(User user, in ClientMessage msg);

        /// <summary>
        /// Main game logic processing method. Invoked by the server for each user (both connected and disconnected).
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ellapsedMilliseconds"></param>
        /// <returns></returns>
        Task ProcessUserTurn(User user, int ellapsedMilliseconds);

        /// <summary>
        /// Invoked by the server after the turn processing has finished and before the next turn.
        /// If no implementation is needed return <see cref="Task.CompletedTask"/>.
        /// </summary>
        /// <returns></returns>
        Task OnTurnComplete();
    }
}
