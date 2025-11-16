using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;

namespace Application.Interfaces;

public interface IProjectService
{
    // CRUD-операции
    Task<int> CreateProjectAsync(CreateProjectRequest request, int createdByUserId,
        CancellationToken cancellationToken);

    Task<IEnumerable<ShortProjectResponse>> GetAllProjectsAsync(CancellationToken cancellationToken);
    Task<ProjectResponse> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken);
    Task UpdateProjectAsync(int projectId, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task DeleteProjectAsync(int projectId, CancellationToken cancellationToken);
}