namespace MultiplayerHost.Abstract
{
    using System;
    using System.Threading.Tasks;
    using MultiplayerHost.Messages;

    public delegate void PlayerDisconnectedEventHandler(object sender, PlayerDisconnectedArgs e);
    public delegate void PlayerConnectingEventHandler(object sender, PlayerConnectingArgs e);

    public record PlayerDisconnectedArgs(int PlayerId);
    public class PlayerConnectingArgs : EventArgs
    {
        public PlayerConnectingArgs(int playerId)
        {
            PlayerId = playerId;
        }
        public int PlayerId { get; init; }
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
