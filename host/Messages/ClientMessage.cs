using System;

namespace MultiplayerHost.Messages
{
    /// <summary>
    /// Client 2 Server message.
    /// </summary>
    public readonly struct ClientMessage
    {
        /// <summary>
        /// ID of logged in user.
        /// </summary>
        public int PlayerId { get; init; }

        /// <summary>
        /// Client generated unique ID. 
        /// This field is ignored by the server Server. It is free to use by game implementation.
        /// </summary>
        public int Cid { get; init; }

        /// <summary>
        /// Client generated message creation time in ticks.
        /// This field is ignored by the server Server. It is free to use by game implementation.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Op code of the request message as defined by the custom game implementation.
        /// </summary>
        public int Code { get; init; }

        /// <summary>
        /// Payload, used by the game implementation.
        /// </summary>
        public string Data { get; init; }
    }
}
