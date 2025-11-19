using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;

namespace Infrastructure.Services;

public class StagesService : IStageService
{
    public Task<int> CreateStageAsync(CreateStageRequest request, int projectId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ShortStageResponse>> GetAllStagesByProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<StageResponse> GetStageDetailAsync(int stageId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UpdateStageAsync(int stageId, UpdateStageRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteProjectAsync(int stageId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}