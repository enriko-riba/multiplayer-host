namespace MultiplayerHost.Domain
{
    using MultiplayerHost.Messages;
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
        public bool WaitOnMessage => responseBufferSignal.WaitOne(SERVER_MESSAGE_WAIT_TIMEOUT);

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
}
