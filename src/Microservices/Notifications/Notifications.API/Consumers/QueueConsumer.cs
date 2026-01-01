using Moser.Archetype.Notifications.API.Features.Agents;

using Azure.Storage.Queues;

using Microsoft.AspNetCore.SignalR;

using System.Text.Json;

namespace Moser.Archetype.Notifications.API.Consumers;

public class QueueConsumer : BackgroundService
{
    private readonly QueueClient _queue;
    private readonly IHubContext<AgentsHub> _hub;

    public QueueConsumer(QueueClient queue, IHubContext<AgentsHub> hub)
    {
        _queue = queue;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var messages = await _queue.ReceiveMessagesAsync(10, cancellationToken: ct);
            foreach (var msg in messages.Value)
            {
                var evt = JsonSerializer.Deserialize<AgentDataFetchedEvent>(msg.Body.ToString());
                await _hub.Clients.Group(evt.UserId.ToString())
                    .SendAsync("AgentsUpdated", evt.Agents, cancellationToken: ct);
                await _queue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
            }
            await Task.Delay(2000, ct);
        }
    }
}
