using System;

namespace Anima.Blueprint.BuildingBlocks.Application.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
