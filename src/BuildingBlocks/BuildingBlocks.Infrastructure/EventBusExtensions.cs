using Moser.Archetype.BuildingBlocks.Application.Events;
using Moser.Archetype.BuildingBlocks.Infrastructure.EventBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using System.Linq;
using System.Reflection;

namespace Moser.Archetype.BuildingBlocks.Infrastructure;

public static class EventBusExtensions
{
    public static IServiceCollection AddInMemoryEventBus(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.AddScoped<IEventBus, InMemoryEventBus>();
        RegisterHandlers(services, assembly);
        return services;
    }

    public static IServiceCollection AddAzureStorageQueueEventBus(
        this IServiceCollection services,
        IConfiguration config,
        Assembly assembly)
    {
        services.AddSingleton<IEventBus>(sp => new AzureStorageQueueEventBus(
            config.GetConnectionString("StorageQueue")!,
            config["EventBus:QueueName"] ?? "events"));
        RegisterHandlers(services, assembly);
        return services;
    }

    public static IServiceCollection AddAzureServiceBusEventBus(
        this IServiceCollection services,
        IConfiguration config,
        Assembly assembly)
    {
        services.AddSingleton<IEventBus>(sp => new AzureServiceBusEventBus(
            config.GetConnectionString("ServiceBus")!,
            config["EventBus:TopicName"] ?? "events"));
        RegisterHandlers(services, assembly);
        return services;
    }

    public static IServiceCollection AddAzureEventGridEventBus(
        this IServiceCollection services,
        IConfiguration config,
        Assembly assembly)
    {
        services.AddSingleton<IEventBus>(sp => new AzureEventGridEventBus(
            config["EventGrid:Endpoint"]!,
            config["EventGrid:AccessKey"]!));
        RegisterHandlers(services, assembly);
        return services;
    }

    public static IServiceCollection AddAzureEventHubsEventBus(
        this IServiceCollection services,
        IConfiguration config,
        Assembly assembly)
    {
        services.AddSingleton<IEventBus>(sp => new AzureEventHubsEventBus(
            config.GetConnectionString("EventHubs")!,
            config["EventBus:HubName"] ?? "events"));
        RegisterHandlers(services, assembly);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList()
            .ForEach(x => services.AddScoped(x.Interface, x.Implementation));
    }
}
