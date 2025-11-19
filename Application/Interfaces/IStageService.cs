using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;

namespace Application.Interfaces;

public interface IStageService
{
    // CRUD-операции
    Task<int> CreateStageAsync(CreateStageRequest request, int projectId,
        CancellationToken cancellationToken);

    Task<IEnumerable<ShortStageResponse>> GetAllStagesByProjectAsync(int projectId, CancellationToken cancellationToken);
    Task<StageResponse> GetStageDetailAsync(int stageId, CancellationToken cancellationToken);
    Task UpdateStageAsync(int stageId, UpdateStageRequest request, CancellationToken cancellationToken);
    Task DeleteProjectAsync(int stageId, CancellationToken cancellationToken);
}