using System;

namespace Moser.Archetype.BuildingBlocks.Application.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
