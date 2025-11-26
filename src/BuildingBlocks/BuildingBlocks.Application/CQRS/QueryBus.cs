using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.BuildingBlocks.Application.CQRS;

internal sealed class QueryBus : IQueryBus
{
    private readonly IServiceProvider _provider;

    public QueryBus(IServiceProvider provider) => _provider = provider;

    public Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _provider.GetRequiredService(handlerType);

        return (Task<TResult>)handlerType
            .GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle))!
            .Invoke(handler, new object[] { query, ct })!;
    }
}
