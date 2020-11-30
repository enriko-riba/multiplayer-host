namespace MultiplayerHost.Domain
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public partial class Server
    {
        /// <summary>
        /// Message dispatching loop. Sends queued server messages to clients via <see cref="Abstract.IConnectionManager"/>.
        /// </summary>
        private async Task DispatcherLoop()
        {
            Thread.CurrentThread.Name = nameof(DispatcherLoop);
            logger.WithMethodName().LogInformation(nameof(DispatcherLoop) + " started");
            while (IsRunning)
            {
                try
                {
                    if (responseBuffer.WaitOnMessage)
                    {
                        while (responseBuffer.Read(out var msg))
                        {
                            await context.ConnectionManager.SendMessage(in msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.WithMethodName().LogError(ex, "error dispatching response");
                }
            }
            logger.WithMethodName().LogWarning(nameof(DispatcherLoop) + " loop ended");
        }
    }
}
