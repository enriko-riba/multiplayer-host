namespace MultiplayerHost.Messages
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Server 2 client message.
    /// </summary>
    public readonly struct ServerMessage
    {
        /// <summary>
        /// Server tick (turn counter).
        /// </summary>
        public ulong Tick { get; init; }

        /// <summary>
        /// Server time in ticks when the message was generated.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Op code of the response message as defined by the custom game implementation.
        /// </summary>
        public int Code { get; init; }

        /// <summary>
        /// Response payload, used by the game implementation.
        /// </summary>
        public string Data { get; init; }

        /// <summary>
        /// Defines how to use the <see cref="ServerMessage.Targets"/> user id's.
        /// </summary>
        [JsonIgnore]
        public TargetKind TargetKind { get; init; }

        /// <summary>
        /// Array of user id's that are used to calculate the receivers of this message. 
        /// The user id's are always used in the context of <see cref="ServerMessage.TargetKind"/>.
        /// </summary>
        [JsonIgnore]
        public int[] Targets { get; init; }
    }
}
