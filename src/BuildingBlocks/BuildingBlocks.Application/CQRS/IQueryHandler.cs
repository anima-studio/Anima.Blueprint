using Anima.Blueprint.BuildingBlocks.Application.CQRS;

using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Application;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
