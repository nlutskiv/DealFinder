using DealFinder.Api.Models.PcBuild;

namespace DealFinder.Api.Services.PcBuild;

public interface IPcBuildService
{
    Task<PcBuildResult> GenerateBuildAsync(PcBuildRequest request);
}
