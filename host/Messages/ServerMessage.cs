namespace MultiplayerHost.Messages;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Server 2 client message.
/// </summary>
public readonly struct ServerMessage
{
    /// <summary>
    /// Creates a server message with the required host-facing fields.
    /// </summary>
    /// <param name="tick">Server tick during which the message was created.</param>
    /// <param name="created">UTC timestamp in ticks when the message was generated.</param>
    /// <param name="code">Game-specific response code.</param>
    /// <param name="data">Payload owned by the game implementation.</param>
    /// <param name="targetKind">How to interpret the targets collection.</param>
    /// <param name="targets">Target user ids used with the selected target kind.</param>
    public ServerMessage(ulong tick, long created, int code, string data, TargetKind targetKind, int[] targets)
    {
        Tick = tick;
        Created = created;
        Code = code;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        TargetKind = targetKind;
        Targets = targets ?? throw new ArgumentNullException(nameof(targets));
    }

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
    public string Data { get; init; } = string.Empty;

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
    public int[] Targets { get; init; } = [];
}
