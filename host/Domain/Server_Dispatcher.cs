namespace MultiplayerHost.Domain;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public partial class Server
{
    private static readonly EventId DispatcherDiagnosticsEventId = new(1200, nameof(DispatcherDiagnosticsEventId));

    /// <summary>
    /// Message dispatching loop. Sends queued server messages to clients via <see cref="Abstract.IConnectionManager"/>.
    /// </summary>
    private async Task DispatcherLoop(CancellationToken cancellationToken)
    {
        Thread.CurrentThread.Name = nameof(DispatcherLoop);
        logger.LogInformation(nameof(DispatcherLoop) + " started");

        while (!cancellationToken.IsCancellationRequested || responseBuffer.HasPendingMessages)
        {
            try
            {
                if (responseBuffer.WaitOnMessage(cancellationToken))
                {
                    logger.LogDebug(DispatcherDiagnosticsEventId, "Dispatcher wake-up. PendingResponses={PendingResponses}", responseBuffer.Count);

                    while (responseBuffer.Read(out var msg))
                    {
                        await context.ConnectionManager.SendMessage(in msg);
                        RecordServerMessageDispatched();
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                while (responseBuffer.Read(out var msg))
                {
                    await context.ConnectionManager.SendMessage(in msg);
                    RecordServerMessageDispatched();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "error dispatching response");
            }
        }
        logger.LogWarning(nameof(DispatcherLoop) + " loop ended");
    }
}
