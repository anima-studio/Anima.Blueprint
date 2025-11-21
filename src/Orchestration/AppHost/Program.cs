using Anima.Blueprint.Catalog.Infrastructure;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Anima.Blueprint.AppHost;

public static class Program
{
    public const string PostgresContainerName = "platform-postgres-container";
    public const string PostgresDbName = "postgres-db";
    public const string CatalogApiName = "catalog-api";

    static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var cache = builder.AddRedis("cache");

        var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");

        builder.AddProject<Projects.Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(catalogApi)
            .WaitFor(catalogApi);

        var app = builder.Build();

        app.Run();
    }

    public static IResourceBuilder<PostgresServerResource> AddPostgresContainer(IDistributedApplicationBuilder builder, bool includePostgresAdministrationPlatform = false)
    {
        var postgresServerBuilder = builder.AddPostgres(PostgresContainerName);

        if (includePostgresAdministrationPlatform)
        {
            postgresServerBuilder.WithPgAdmin();
        }

        return postgresServerBuilder;
    }

    public static IResourceBuilder<PostgresDatabaseResource> AddPostgresDb(IResourceBuilder<PostgresServerResource> postgresContainer)
    {
        return postgresContainer.AddDatabase(PostgresDbName, FakeDbContext.DbName);
    }
}
