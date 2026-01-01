using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.BuildingBlocks.Infrastructure.Persistence;

public abstract class DbContextBase : DbContext
{
    protected DbContextBase(DbContextOptions options) : base(options) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(256);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
