using Anima.Blueprint.BuildingBlocks.Application.CQRS;

using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Application;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}
