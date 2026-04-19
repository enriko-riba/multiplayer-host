namespace MultiplayerHost.Domain;

using MultiplayerHost.Messages;
using System;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// Handles writing and reading server messages.
/// </summary>
internal sealed class ResponseBuffer
{
    private const int SERVER_MESSAGE_WAIT_TIMEOUT = 250;
    private readonly AutoResetEvent responseBufferSignal = new(false);
    private readonly ConcurrentQueue<ServerMessage> responseBuffer = new();

    /// <summary>
    /// Blocks the current thread until a write operation signals that new messages are available.
    /// </summary>
    public bool WaitOnMessage(CancellationToken cancellationToken)
    {
        var waitHandles = new WaitHandle[] { responseBufferSignal, cancellationToken.WaitHandle };
        var signalIndex = WaitHandle.WaitAny(waitHandles, SERVER_MESSAGE_WAIT_TIMEOUT);

        return signalIndex switch
        {
            WaitHandle.WaitTimeout => responseBuffer.Count > 0,
            0 => true,
            1 => throw new OperationCanceledException(cancellationToken),
            _ => false
        };
    }

    /// <summary>
    /// Returns true when outbound messages are still queued.
    /// </summary>
    public bool HasPendingMessages => !responseBuffer.IsEmpty;

    /// <summary>
    /// Gets the current number of queued outbound messages.
    /// </summary>
    public int Count => responseBuffer.Count;

    /// <summary>
    /// Writes a server message to the buffer.
    /// </summary>
    /// <param name="message"></param>
    public void Write(in ServerMessage message)
    {
        responseBuffer.Enqueue(message);
        responseBufferSignal.Set();
    }

    /// <summary>
    /// Reads the next ServerMessage from the buffer.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>true if the message was removed from the buffer, otherwise false</returns>
    public bool Read(out ServerMessage message) => responseBuffer.TryDequeue(out message);
}
