using Moser.Archetype.BuildingBlocks.Application.Events;

using Azure.Messaging.ServiceBus;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Infrastructure.EventBus;

public sealed class AzureServiceBusEventBus : IEventBus
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusEventBus(string connectionString, string topicName)
        => _sender = new ServiceBusClient(connectionString).CreateSender(topicName);

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var json = JsonSerializer.Serialize(@event);
        await _sender.SendMessageAsync(new ServiceBusMessage(json), ct);
    }
}
