using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Application.CQRS;

public interface IQueryBus
{
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
