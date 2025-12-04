using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Сервис для управления проектами. Определяет интерфейс IProjectService
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ProjectManagementDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ProjectManagementDbContext context,
        INotificationService notificationService,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает детальную информацию о проекте по его идентификатору.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
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
                Description = p.Description,
                Budget = p.Budget,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
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

    /// <summary>
    /// Обновляет свойства проекта, включая статус, основные поля и даты.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="request">Модель с изменяемыми полями проекта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task UpdateProjectAsync(int projectId, UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var project = await _context.Projects
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var oldStatus = project.Status;
        var oldName = project.Name;

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

        if (request.Name is not null)
        {
            project.Name = request.Name;
        }

        if (request.Description is not null)
        {
            project.Description = request.Description;
        }

        if (request.Budget.HasValue)
        {
            if (request.Budget.Value <= 0)
            {
                throw new ArgumentException("Бюджет должен быть больше нуля.");
            }

            project.Budget = request.Budget.Value;
        }

        _logger.LogDebug(
            "Обновление проекта {ProjectId}. StartDate.HasValue: {StartDateHasValue}, StartDate: {StartDate}, EndDate.HasValue: {EndDateHasValue}, EndDate: {EndDate}",
            projectId,
            request.StartDate.HasValue,
            request.StartDate.HasValue ? request.StartDate.Value.ToString("O") : "null",
            request.EndDate.HasValue,
            request.EndDate.HasValue ? request.EndDate.Value.ToString("O") : "null");

        if (request.StartDate.HasValue && request.StartDate.Value != default(DateTime) && request.StartDate.Value.Year > 1900)
        {
            var startDateUtc = request.StartDate.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc)
                : request.StartDate.Value.ToUniversalTime();
            
            project.StartDate = startDateUtc;
            _logger.LogInformation("Обновлена дата начала проекта {ProjectId}: {StartDate} (UTC)", projectId, project.StartDate.ToString("O"));
        }
        else if (request.StartDate.HasValue)
        {
            _logger.LogWarning("Пропущена некорректная дата начала проекта {ProjectId}: {StartDate}", projectId, request.StartDate.Value);
        }

        if (request.EndDate.HasValue && request.EndDate.Value != default(DateTime) && request.EndDate.Value.Year > 1900)
        {
            var endDateUtc = request.EndDate.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc)
                : request.EndDate.Value.ToUniversalTime();
            
            if (endDateUtc < project.StartDate)
            {
                throw new ArgumentException("Дата завершения не может быть раньше даты начала.");
            }

            project.EndDate = endDateUtc;
            _logger.LogInformation("Обновлена дата завершения проекта {ProjectId}: {EndDate} (UTC)", projectId, project.EndDate.ToString("O"));
        }
        else if (request.EndDate.HasValue)
        {
            _logger.LogWarning("Пропущена некорректная дата завершения проекта {ProjectId}: {EndDate}", projectId, request.EndDate.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Checking notification condition for project {ProjectId}. RequestedStatus: {RequestedStatus}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
            project.ProjectId,
            request.Status,
            oldStatus,
            project.Status);

        if (request.Status is not null && oldStatus != project.Status)
        {
            _logger.LogInformation(
                "Project status changed. Creating notification for user {UserId}, project {ProjectId} (from {OldStatus} to {NewStatus})",
                project.CreatedByUserId,
                project.ProjectId,
                oldStatus,
                project.Status);

            await _notificationService.CreateAndSendNotificationAsync(
                project.CreatedByUserId,
                project.ProjectId,
                $"Статус проекта '{project.Name}' изменен с '{oldStatus}' на '{project.Status}'.",
                cancellationToken);

            _logger.LogInformation(
                "Status-change notification created for user {UserId}, project {ProjectId}",
                project.CreatedByUserId,
                project.ProjectId);
        }
        else
        {
            _logger.LogDebug(
                "Notification condition not met for project {ProjectId}. StatusInRequestNull: {IsNull}, StatusChanged: {StatusChanged}",
                project.ProjectId,
                request.Status is null,
                oldStatus != project.Status);
        }
    }

    /// <summary>
    /// Создает новый проект и возвращает его идентификатор.
    /// </summary>
    /// <param name="request">Модель с данными создаваемого проекта.</param>
    /// <param name="createdByUserId">Идентификатор пользователя‑создателя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<int> CreateProjectAsync(CreateProjectRequest request, int createdByUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Создание проекта. StartDate: {StartDate}, EndDate: {EndDate}",
            request.StartDate.ToString("O"),
            request.EndDate.ToString("O"));

        var startDateUtc = request.StartDate.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc)
            : request.StartDate.ToUniversalTime();
        
        var endDateUtc = request.EndDate.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc)
            : request.EndDate.ToUniversalTime();

        if (endDateUtc < startDateUtc)
        {
            throw new ArgumentException("Дата завершения не может быть раньше даты начала.");
        }

        var newProject = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Budget = request.Budget,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Status = ProjectStatus.Planning,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Проект подготовлен к созданию. StartDate (UTC): {StartDate}, EndDate (UTC): {EndDate}",
            newProject.StartDate.ToString("O"),
            newProject.EndDate.ToString("O"));

        await _context.Projects.AddAsync(newProject, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project created. ProjectId: {ProjectId}, Name: {Name}, CreatedByUserId: {UserId}",
            newProject.ProjectId,
            newProject.Name,
            createdByUserId);

        try
        {
            await _notificationService.CreateAndSendNotificationAsync(
                createdByUserId,
                newProject.ProjectId,
                $"Проект '{newProject.Name}' был создан.",
                cancellationToken);

            _logger.LogInformation(
                "Creation notification sent successfully for project {ProjectId} to user {UserId}",
                newProject.ProjectId,
                createdByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while sending creation notification for project {ProjectId} to user {UserId}",
                newProject.ProjectId,
                createdByUserId);
        }

        return newProject.ProjectId;
    }

    /// <summary>
    /// Возвращает список всех проектов в укороченном представлении.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
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

    /// <summary>
    /// Удаляет проект по его идентификатору.
    /// </summary>
    /// <param name="projectId">Идентификатор удаляемого проекта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
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
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден для удаления.");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}