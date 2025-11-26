using Anima.Blueprint.BuildingBlocks.Application.Events;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.EventBus;

public sealed class AzureQueueEventBus : IEventBus
{
    private readonly QueueClient _queue;

    public AzureQueueEventBus(string connectionString, string queueName)
        => _queue = new QueueClient(connectionString, queueName);

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
    {
        var json = JsonSerializer.Serialize(@event);
        await _queue.SendMessageAsync(json, ct);
    }
}
