namespace MultiplayerHost.Abstract
{
    using System;
    using System.Threading.Tasks;
    using MultiplayerHost.Messages;

    /// <summary>
    /// Contract for <see cref="IConnectionManager.PlayerDisconnected"/> event handler.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void PlayerDisconnectedEventHandler(object sender, PlayerDisconnectedArgs e);

    /// <summary>
    /// Contract for <see cref="IConnectionManager.PlayerConnecting"/> event handler.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void PlayerConnectingEventHandler(object sender, PlayerConnectingArgs e);

    /// <summary>
    /// PlayerDisconnected event argument.
    /// </summary>
    public record PlayerDisconnectedArgs(int PlayerId);

    /// <summary>
    /// PlayerConnecting event argument.
    /// </summary>
    public class PlayerConnectingArgs : EventArgs
    {
        public PlayerConnectingArgs(int playerId)
        {
            PlayerId = playerId;
        }
        /// <summary>
        /// Id of the player trying to connect.
        /// </summary>
        public int PlayerId { get; init; }

        /// <summary>
        /// Cancels the player connection if set to true.
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Defines client connection operations. 
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Raised when a new user connection is about to be accepted. 
        /// If the subscriber sets the <see cref="PlayerConnectingArgs.Cancel"/> to true, the connection must be dropped.
        /// </summary>
        event PlayerConnectingEventHandler PlayerConnecting;

        /// <summary>
        /// Raised when a user disconnect is detected.
        /// Used by subscribers to clean up user state.
        /// </summary>
        event PlayerDisconnectedEventHandler PlayerDisconnected;

        /// <summary>
        /// Forcefully disconnects the player.
        /// </summary>
        /// <param name="playerId"></param>
        void DisconnectPlayer(int playerId);

        /// <summary>
        /// Sends the server message to the client identified by the <see cref="ServerMessage.TargetKind"/>
        /// and <see cref="ServerMessage.Targets"/> combination.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendMessage(in ServerMessage message);
    }
}
