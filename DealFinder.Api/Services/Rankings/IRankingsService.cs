using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Rankings;

public interface IRankingsService
{
    Task<List<RankingItemDto>> GetTopDealsAsync(int perCategory = 5);
}
