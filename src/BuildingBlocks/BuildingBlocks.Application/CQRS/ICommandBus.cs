using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Application.CQRS;

public interface ICommandBus
{
    Task Send(ICommand command, CancellationToken ct = default);
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}
