using Anima.Blueprint.BuildingBlocks.Application.Events;
using Anima.Blueprint.BuildingBlocks.Infrastructure.EventBus;

using Microsoft.Extensions.DependencyInjection;

using System.Linq;
using System.Reflection;

namespace Anima.Blueprint;

public static class EventBusExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, Assembly assembly)
    {
        // Development: In-memory
        services.AddScoped<IEventBus, InMemoryEventBus>();

        // Production: Azure Queue (switch via config)
        // services.AddSingleton<IEventBus>(sp => 
        //     new AzureQueueEventBus(config["StorageQueue"], "events"));

        // Register handlers
        var handlers = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));

        foreach (var handler in handlers)
        {
            var iface = handler.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));
            services.AddScoped(iface, handler);
        }

        return services;
    }
}
