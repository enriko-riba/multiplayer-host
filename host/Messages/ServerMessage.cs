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

        [JsonIgnore]
        public TargetKind TargetKind { get; init; }

        [JsonIgnore]
        public int[] Targets { get; init; }
    }
}
