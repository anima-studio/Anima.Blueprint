using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Application.Events;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task Handle(TEvent @event, CancellationToken ct);
}
