namespace MultiplayerHost.Domain
{
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
        /// <summary>
        /// Main server loop, runs until the stop request
        /// </summary>
        private async Task MainLoop()
        {
            Thread.CurrentThread.Name = nameof(MainLoop);
            logger.LogInformation(nameof(MainLoop) + " started. Tick duration: {TickDuration}", TICK_DURATION);

            var sw = new Stopwatch();
            sw.Start();
            long tickEnd = sw.ElapsedMilliseconds;

            while (IsRunning)
            {
                tickCounter++;
                var tickStart = sw.ElapsedMilliseconds;
                var ellapsedMilliseconds = (int)(tickStart - tickEnd);

                try
                {
                    requestBuffer.SwapBuffers();
                    await context.TurnProcessor.OnTurnStart(tickCounter);
                    ProcessClientMessages();
                    await ProcessAllUsers(ellapsedMilliseconds);
                    await context.TurnProcessor.OnTurnComplete();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error while processing turn {TickCounter}", tickCounter);
                }

                tickEnd = sw.ElapsedMilliseconds;
                var duration = (int)(tickEnd - tickStart);
                var sleepTimeMillis = Math.Max(TICK_DURATION - duration, 1);
                await Task.Delay(sleepTimeMillis);
            }
            logger.LogWarning(nameof(MainLoop) + " ended");
        }

        private async Task ProcessAllUsers(int ellapsedMilliseconds)
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
                        await context.TurnProcessor.ProcessUserTurn(user, ellapsedMilliseconds);
                        if (ShouldSaveUser(user)) await context.Repository.SaveUserAsync(user);
                    }
                    else
                    {
                        logger.LogError("could not find user {PlayerId}", key);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "User " + key);
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
                }
                else
                {
                    logger.LogError("could not find user {UserId}", msg.UserId);
                }
            }
        }
    }
}
