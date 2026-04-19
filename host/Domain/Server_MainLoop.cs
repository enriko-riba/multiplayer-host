namespace MultiplayerHost.Domain;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Consumes messages from the response buffer and sends them to the connection handler.
/// All message processing is run on a dedicated background thread.
/// </summary>
public partial class Server
{
    private static readonly EventId TurnDiagnosticsEventId = new(1100, nameof(TurnDiagnosticsEventId));
    private const int TurnDiagnosticsInterval = 100;

    /// <summary>
    /// Main server loop, runs until the stop request
    /// </summary>
    private async Task MainLoop(CancellationToken cancellationToken)
    {
        Thread.CurrentThread.Name = nameof(MainLoop);
        logger.LogInformation(nameof(MainLoop) + " started. Tick duration: {TickDuration}", Context.TurnTimeMillis);

        var sw = new Stopwatch();
        sw.Start();
        long tickEnd = sw.ElapsedMilliseconds;

        while (!cancellationToken.IsCancellationRequested)
        {
            uint currentTick = unchecked((uint)Interlocked.Increment(ref tickCounter));
            var tickStart = sw.ElapsedMilliseconds;
            var elapsedMilliseconds = (int)(tickStart - tickEnd);

            try
            {
                requestBuffer.SwapBuffers();
                await context.TurnProcessor.OnTurnStart(currentTick, elapsedMilliseconds);
                ProcessClientMessages();
                await ProcessAllUsers(elapsedMilliseconds);
                await context.TurnProcessor.OnTurnComplete();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "error while processing turn {TickCounter}", currentTick);
            }

            tickEnd = sw.ElapsedMilliseconds;
            var duration = (int)(tickEnd - tickStart);
            var sleepTimeMillis = Math.Max(Context.TurnTimeMillis - duration, 1);

            if (currentTick % TurnDiagnosticsInterval == 0)
            {
                logger.LogDebug(
                    TurnDiagnosticsEventId,
                    "Turn {TickCounter} completed. DurationMs={TurnDurationMs}, SleepMs={SleepDurationMs}, ActiveUsers={ActiveUsers}, PendingResponses={PendingResponses}",
                    currentTick,
                    duration,
                    sleepTimeMillis,
                    users.Count,
                    responseBuffer.Count);
            }

            try
            {
                await Task.Delay(sleepTimeMillis, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
        logger.LogWarning(nameof(MainLoop) + " ended");
    }

    private async Task ProcessAllUsers(int elapsedMilliseconds)
    {
        //  enumerate users in a thread safe way
        //  TODO: check if this is a bottleneck (refactor to linked list)
        var keys = users.Keys.ToArray();    
        foreach (var key in keys)
        {
            try
            {
                if (users.TryGetValue(key, out var user))
                {
                    await context.TurnProcessor.ProcessUserTurn(user, elapsedMilliseconds);
                    if (ShouldSaveUser(user))
                    {
                        await context.Repository.SaveUserAsync(user);
                        RecordUserPersisted(user);
                    }
                }
                else
                {
                    logger.LogError("could not find user {PlayerId}", key);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User {PlayerId}", key);
            }
        }
    }

    /// <summary>
    /// Reads all messages from the request buffer and sends them to the turn processor.
    /// </summary>
    /// <returns></returns>
    private void ProcessClientMessages()
    {
        while (requestBuffer.TryRead(out var msg))
        {
            if (users.TryGetValue(msg.UserId, out var user))
            {
                context.TurnProcessor.ProcessClientMessage(user, in msg);
                RecordClientMessageProcessed();
            }
            else
            {
                logger.LogError("could not find user {UserId}", msg.UserId);
            }
        }
    }
}
