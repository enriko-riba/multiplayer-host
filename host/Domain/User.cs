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

        [JsonIgnore]
        public System.DateTime LastSaved { get; set; }
        
        [JsonIgnore]
        public bool IsDirty { get; set; }
        
        [JsonIgnore]
        public DateTime? IsOnlineSince { get; internal set; }
    }
}
