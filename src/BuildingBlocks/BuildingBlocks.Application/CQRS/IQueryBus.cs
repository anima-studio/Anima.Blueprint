using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Application.CQRS;

public interface IQueryBus
{
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
