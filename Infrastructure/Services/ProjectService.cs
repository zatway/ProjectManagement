using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Сервис для управления проектами. Определяет интерфейс IProjectService
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ProjectManagementDbContext _context;

    public ProjectService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectResponse> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => new ProjectResponse
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Budget = p.Budget,
                Status = p.Status.ToString(),
                StagesCount = p.Stages.Count
            })
            .FirstOrDefaultAsync();

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        return project;
    }

    public async Task UpdateProjectAsync(int projectId, UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        if (request.Status is not null)
        {
            if (Enum.TryParse<ProjectStatus>(request.Status, true, out var newStatus))
            {
                project.Status = newStatus;
            }
            else
            {
                throw new ArgumentException(
                    $"Статус '{request.Status}' не является корректным значением для ProjectStatus.");
            }
        }

        // Используем оператор 'is not null' для ссылочных типов (string?)
        if (request.Name is not null)
        {
            project.Name = request.Name;
        }

        if (request.Description is not null)
        {
            project.Description = request.Description;
        }

        // Используем .HasValue для значимых nullable-типов (decimal?, DateTime?)
        if (request.Budget.HasValue)
        {
            if (request.Budget.Value <= 0)
            {
                throw new ArgumentException("Бюджет должен быть больше нуля.");
            }

            project.Budget = request.Budget.Value;
        }

        if (request.StartDate.HasValue)
        {
            project.StartDate = request.StartDate.Value;
        }

        if (request.EndDate.HasValue)
        {
            // 💡Проверка, что EndDate > StartDate
            if (request.EndDate.Value < project.StartDate)
            {
                throw new ArgumentException("Дата завершения не может быть раньше даты начала.");
            }

            project.EndDate = request.EndDate.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CreateProjectAsync(CreateProjectRequest request, int createdByUserId,
        CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("Дата завершения не может быть раньше даты начала.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var newProject = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Budget = request.Budget,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ProjectStatus.Planning,
            CreatedByUserId = createdByUserId,
        };
        await _context.Projects.AddAsync(newProject, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return newProject.ProjectId;
    }

    public async Task<IEnumerable<ShortProjectResponse>> GetAllProjectsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projects = await _context.Projects
            .AsNoTracking()
            .Select(p => new ShortProjectResponse
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Status = p.Status.ToString(),
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            })
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task DeleteProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        if (projectId <= 0)
        {
            throw new ArgumentException("Идентификатор проекта должен быть положительным числом.", nameof(projectId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);

        if (project == null)
        {
            // Возвращаем исключение, которое в контроллере будет преобразовано в 404 Not Found
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден для удаления.");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}