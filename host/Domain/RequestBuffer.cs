namespace MultiplayerHost.Domain
{
    using MultiplayerHost.Messages;
    using System.Collections.Generic;

    /// <summary>
    /// Handles simultaneous reading and writing client messages.
    /// Write operations (client 2 server messages) are using one buffer while the read operations (turn processing) are using the other. 
    /// Once per turn, after all read operations are processed the buffers are swapped.
    /// </summary>
    internal sealed class RequestBuffer
    {
        private int writeBuffer = 0;
        private int readBuffer = 1;

        /// <summary>
        /// Holds two client message buffers. One buffer is write only while the other is read only. 
        /// The buffers are swapped each turn.
        /// </summary>
        private readonly Queue<ClientMessage>[] swapChain = new[] {
            new Queue<ClientMessage>(1024),
            new Queue<ClientMessage>(1024)
        };

        /// <summary>
        /// Enqueues a client message for processing.
        /// </summary>
        /// <param name="message"></param>
        public void Write(in ClientMessage message)
        {
            swapChain[writeBuffer].Enqueue(message);
        }

        /// <summary>
        /// Reads the next client message from the read buffer.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool TryRead(out ClientMessage message) => swapChain[readBuffer].TryDequeue(out message);


        /// <summary>
        /// Swaps the read and write buffers.
        /// </summary>
        public void SwapBuffers()
        {
            (writeBuffer, readBuffer) = (readBuffer, writeBuffer);
        }
    }
}
