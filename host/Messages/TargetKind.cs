namespace MultiplayerHost.Messages
{
    /// <summary>
    /// Defines how target user id's are interpreted during message sending.
    /// </summary>
    public enum TargetKind
    {
        /// <summary>
        /// Message is sent to all clients.
        /// </summary>
        All,

        /// <summary>
        /// Message is sent only to clients in the targets array.
        /// </summary>
        TargetList,

        /// <summary>
        /// Message is sent to all clients except to those in the targets array.
        /// </summary>
        AllExcept
    }
}
