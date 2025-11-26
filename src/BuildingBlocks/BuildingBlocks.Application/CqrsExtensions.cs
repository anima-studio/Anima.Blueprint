using Anima.Blueprint.BuildingBlocks.Application.CQRS;

using Microsoft.Extensions.DependencyInjection;

using System.Linq;
using System.Reflection;

namespace Anima.Blueprint.BuildingBlocks.Application;

public static class CqrsExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        // Register command handlers
        var commandHandlers = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

        foreach (var handler in commandHandlers)
        {
            var interfaces = handler.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)));

            foreach (var iface in interfaces)
                services.AddScoped(iface, handler);
        }

        // Register query handlers
        var queryHandlers = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

        foreach (var handler in queryHandlers)
        {
            var iface = handler.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
            services.AddScoped(iface, handler);
        }

        return services;
    }
}
