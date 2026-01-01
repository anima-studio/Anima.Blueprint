using Moser.Archetype.BuildingBlocks.Application.Events;

using System;

namespace Moser.Archetype.BuildingBlocks.Application.IntegrationEvents;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
