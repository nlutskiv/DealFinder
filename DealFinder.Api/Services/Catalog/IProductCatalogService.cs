using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Catalog;

public interface IProductCatalogService
{
    Task<List<CatalogProductDto>> GetActiveCatalogAsync();
    Task<ProductCatalog?> GetByIdAsync(int id);
}