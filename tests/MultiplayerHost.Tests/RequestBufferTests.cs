namespace MultiplayerHost.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;
using Xunit;

public class RequestBufferTests
{
    [Fact]
    public async Task SwapBuffers_PreservesAllMessagesUnderConcurrentWrites()
    {
        var buffer = new RequestBuffer();
        const int producerCount = 4;
        const int messagesPerProducer = 250;

        var producers = Enumerable.Range(0, producerCount)
            .Select(producerId => Task.Run(() =>
            {
                for (var i = 0; i < messagesPerProducer; i++)
                {
                    buffer.Write(new ClientMessage(producerId, i, $"{producerId}:{i}")
                    {
                        Cid = i,
                        Created = i
                    });
                }
            }))
            .ToArray();

        await Task.WhenAll(producers);

        buffer.SwapBuffers();

        var drainedMessages = new List<ClientMessage>();
        while (buffer.TryRead(out var message))
        {
            drainedMessages.Add(message);
        }

        Assert.Equal(producerCount * messagesPerProducer, drainedMessages.Count);
        Assert.Equal(producerCount * messagesPerProducer, drainedMessages.Select(message => message.Data).Distinct().Count());
    }
}
