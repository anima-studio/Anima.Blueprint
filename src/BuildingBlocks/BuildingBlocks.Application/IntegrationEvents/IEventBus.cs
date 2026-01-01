using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Application.Events;

public interface IEventBus
{
    Task Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent;
}
