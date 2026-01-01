using Moser.Archetype.BuildingBlocks.Application.Events;

using Azure.Storage.Queues;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Infrastructure.EventBus;

public sealed class AzureStorageQueueEventBus : IEventBus
{
    private readonly QueueClient _queue;

    public AzureStorageQueueEventBus(string connectionString, string queueName)
        => _queue = new QueueClient(connectionString, queueName);

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
    {
        var json = JsonSerializer.Serialize(@event);
        await _queue.SendMessageAsync(json, ct);
    }
}
