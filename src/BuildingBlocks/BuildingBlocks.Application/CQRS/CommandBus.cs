using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Application.CQRS;

internal sealed class CommandBus : ICommandBus
{
    private readonly IServiceProvider _provider;

    public CommandBus(IServiceProvider provider) => _provider = provider;

    public Task Send(ICommand command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = _provider.GetRequiredService(handlerType);

        return (Task)handlerType
            .GetMethod(nameof(ICommandHandler<ICommand>.Handle))!
            .Invoke(handler, new object[] { command, ct })!;
    }

    public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _provider.GetRequiredService(handlerType);

        return (Task<TResult>)handlerType
            .GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle))!
            .Invoke(handler, new object[] { command, ct })!;
    }
}
