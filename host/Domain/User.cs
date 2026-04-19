namespace MultiplayerHost.Domain;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// The user object managed by the GameServer.
/// </summary>
public abstract class User
{
    /// <summary>
    /// Unique user id.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// UTC timestamp of the user's last successful persistence operation.
    /// </summary>
    [JsonIgnore]
    public DateTime LastSaved { get; set; }
    
    /// <summary>
    /// Dirty flag. Dirty users will be saved to a data storage implemented by IRepository.
    /// </summary>
    [JsonIgnore]
    public bool IsDirty { get; set; }
    
    /// <summary>
    /// UTC timestamp of when the user most recently connected.
    /// </summary>
    [JsonIgnore]
    public DateTime? IsOnlineSince { get; internal set; }
}
