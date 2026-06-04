using DealFinder.Api.Models;

namespace DealFinder.Api.Services;

// Search implementation
public interface IProductSearchService
{
    Task<List<ProductResult>> SearchAsync(ProductSearchRequest request);
}