using Moser.Archetype.BuildingBlocks.Application.Events;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Infrastructure.EventBus;

public sealed class AzureEventHubsEventBus : IEventBus
{
    private readonly EventHubProducerClient _producer;

    public AzureEventHubsEventBus(string connectionString, string hubName)
        => _producer = new EventHubProducerClient(connectionString, hubName);

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        using var batch = await _producer.CreateBatchAsync(ct);
        var json = JsonSerializer.Serialize(@event);
        var eventData = new EventData(json);
        batch.TryAdd(eventData);
        await _producer.SendAsync(batch, ct);
    }
}
