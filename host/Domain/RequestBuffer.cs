namespace MultiplayerHost.Domain;

using MultiplayerHost.Messages;
using System.Collections.Generic;

/// <summary>
/// Handles simultaneous reading and writing client messages.
/// Write operations (client 2 server messages) are using one buffer while the read operations (turn processing) are using the other. 
/// Once per turn, after all read operations are processed the buffers are swapped.
/// </summary>
internal sealed class RequestBuffer
{
    private readonly object syncRoot = new();
    private Queue<ClientMessage> writeBuffer;
    private Queue<ClientMessage> readBuffer;

    /// <summary>
    /// Holds two client message buffers. One buffer is write only while the other is read only. 
    /// The buffers are swapped each turn.
    /// </summary>
    private readonly Queue<ClientMessage> bufferA = new(1024);
    private readonly Queue<ClientMessage> bufferB = new(1024);

    /// <summary>
    /// Creates a new request buffer.
    /// </summary>
    public RequestBuffer()
    {
        writeBuffer = bufferA;
        readBuffer = bufferB;
    }

    /// <summary>
    /// Enqueues a client message for processing.
    /// </summary>
    /// <param name="message"></param>
    public void Write(in ClientMessage message)
    {
        lock (syncRoot)
        {
            writeBuffer.Enqueue(message);
        }
    }

    /// <summary>
    /// Reads the next client message from the read buffer.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool TryRead(out ClientMessage message)
    {
        if (readBuffer.Count > 0)
        {
            message = readBuffer.Dequeue();
            return true;
        }

        message = default;
        return false;
    }


    /// <summary>
    /// Swaps the read and write buffers.
    /// </summary>
    public void SwapBuffers()
    {
        lock (syncRoot)
        {
            (writeBuffer, readBuffer) = (readBuffer, writeBuffer);
        }
    }
}
