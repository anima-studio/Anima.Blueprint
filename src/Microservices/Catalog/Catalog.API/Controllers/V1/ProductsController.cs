using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.Catalog.API.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var products = Enumerable.Range(1, pageSize).Select(i => new ProductDto(
            Id: Guid.NewGuid(),
            Name: $"Product {i + (page - 1) * pageSize}",
            Description: $"Description for product {i}",
            Price: Random.Shared.Next(10, 1000),
            Stock: Random.Shared.Next(0, 100)
        ));

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = new ProductDto(
            Id: id,
            Name: "Sample Product",
            Description: "Sample description",
            Price: 99.99m,
            Stock: 50
        );

        return Ok(product);
    }
}

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock
);
