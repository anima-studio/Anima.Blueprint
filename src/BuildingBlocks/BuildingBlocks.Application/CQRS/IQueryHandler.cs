using Moser.Archetype.BuildingBlocks.Application.CQRS;

using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Application;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
