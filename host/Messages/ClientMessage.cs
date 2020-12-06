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
        public int UserId { get; init; }

        /// <summary>
        /// Client generated Id. 
        /// This field is ignored by the server, the game implementation can use it as will.
        /// </summary>
        public int Cid { get; init; }

        /// <summary>
        /// Client generated message creation time in ticks.
        /// This field is ignored by the server, the game implementation can use it as will.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Command code of the message. This field is ignored by the server, the game implementation can use it as will.
        /// </summary>
        public int Code { get; init; }

        /// <summary>
        /// Message payload. This field is ignored by the server, the game implementation can use it as will.
        /// </summary>
        public string Data { get; init; }
    }
}
