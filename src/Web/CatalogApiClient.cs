using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.Web;

public class CatalogApiClient(HttpClient httpClient)
{
    public async Task<ProductDto[]> GetProductsAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<ProductDto[]>(
            $"/api/v1/products?page={page}&pageSize={pageSize}",
            cancellationToken);

        return response ?? Array.Empty<ProductDto>();
    }

    public async Task<ProductDto?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ProductDto>(
            $"/api/v1/products/{id}",
            cancellationToken);
    }
}

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock
);
