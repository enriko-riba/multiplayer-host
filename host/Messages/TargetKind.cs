namespace MultiplayerHost.Messages
{
    public enum TargetKind
    {
        /// <summary>
        /// Message is sent to all clients
        /// </summary>
        All,

        /// <summary>
        /// Message is sent only to clients in the target list.
        /// </summary>
        TargetList,

        /// <summary>
        /// Message is sent to all clients except to those in the targets list.
        /// </summary>
        AllExcept
    }
}
