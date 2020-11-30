namespace MultiplayerHost.Domain
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// The user object managed by the GameServer.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique user id.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Timestamp - users last save time.
        /// </summary>
        [JsonIgnore]
        public DateTime LastSaved { get; set; }
        
        /// <summary>
        /// Dirty flag. Dirty users will be saved to a data storage implemented by IRepository.
        /// </summary>
        [JsonIgnore]
        public bool IsDirty { get; set; }
        
        /// <summary>
        /// Timestamp - users last connect time.
        /// </summary>
        [JsonIgnore]
        public DateTime? IsOnlineSince { get; internal set; }
    }
}
