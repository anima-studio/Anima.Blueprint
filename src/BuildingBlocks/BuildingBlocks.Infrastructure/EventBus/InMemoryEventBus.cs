using Anima.Blueprint.BuildingBlocks.Application.Events;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Infrastructure.EventBus;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _provider;

    public InMemoryEventBus(IServiceProvider provider) => _provider = provider;

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
    {
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
        var handlers = _provider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            await (Task)handlerType
                .GetMethod(nameof(IIntegrationEventHandler<TEvent>.Handle))!
                .Invoke(handler, new object[] { @event, ct })!;
        }
    }
}
