using Moser.Archetype.BuildingBlocks.Infrastructure.Persistence;

namespace Moser.Archetype.Catalog.Infrastructure;

internal class UnitOfWork : UnitOfWorkBase<CatalogDbContext>
{
    public UnitOfWork(CatalogDbContext context) : base(context) { }
}
