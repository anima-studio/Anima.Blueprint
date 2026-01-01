using Microsoft.EntityFrameworkCore;

using Moser.Archetype.BuildingBlocks.Application;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Infrastructure.Persistence;

public abstract class UnitOfWorkBase<TContext> : IUnitOfWork
    where TContext : DbContextBase
{
    protected readonly TContext Context;

    protected UnitOfWorkBase(TContext context)
    {
        Context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Context.SaveChangesAsync(ct);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        var strategy = Context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await Context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}
