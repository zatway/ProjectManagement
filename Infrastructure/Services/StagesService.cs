using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StagesService : IStageService
{
    private readonly ProjectManagementDbContext _context;

    public StagesService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateStageAsync(CreateStageRequest request, int projectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId, cancellationToken);

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var specialistExists = await _context.Users
            .AnyAsync(u => u.UserId == request.SpecialistUserId, cancellationToken);

        if (!specialistExists)
        {
            throw new ArgumentException($"Специалист с ID {request.SpecialistUserId} не найден.");
        }

        if (!Enum.TryParse<StageType>(request.StageType, true, out var stageTypeEnum))
        {
            throw new ArgumentException(
                $"Тип этапа '{request.StageType}' не является корректным значением для StageType.");
        }

        if (!Enum.TryParse<StageStatus>(request.Status, true, out var stageStatusEnum))
        {
            throw new ArgumentException($"Статус '{request.Status}' не является корректным значением для StageStatus.");
        }

        if (request.ProgressPercent == 100)
        {
            stageStatusEnum = StageStatus.Completed;
        }
        else if (request.ProgressPercent > 0 && stageStatusEnum == StageStatus.Pending)
        {
            stageStatusEnum = StageStatus.InProgress;
        }

        var newStage = new Stage
        {
            ProjectId = projectId,
            Name = request.Name,
            StageType = stageTypeEnum,
            Deadline = request.Deadline,
            ProgressPercent = request.ProgressPercent,
            Status = stageStatusEnum,
            SpecialistUserId = request.SpecialistUserId
        };

        await _context.Stages.AddAsync(newStage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newStage.StageId;
    }

    public async Task<IEnumerable<ShortStageResponse>> GetAllStagesByProjectAsync(int projectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Проверка существования проекта
        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId, cancellationToken);

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var stages = await _context.Stages
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .Select(s => new ShortStageResponse
            {
                StageId = s.StageId,
                Name = s.Name,
                StageType = s.StageType.ToString(),
                ProgressPercent = s.ProgressPercent,
                Status = s.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return stages;
    }

    public async Task<StageResponse> GetStageDetailAsync(int stageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stage = await _context.Stages
            .AsNoTracking()
            .Where(s => s.StageId == stageId)
            .Select(s => new StageResponse
            {
                StageId = s.StageId,
                ProjectId = s.ProjectId,
                ProjectName = s.Project.Name,
                Name = s.Name,
                StageType = s.StageType.ToString(),
                Deadline = s.Deadline,
                ProgressPercent = s.ProgressPercent,
                Status = s.Status.ToString(),
                SpecialistUserFullName = s.Specialist.FullName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stage == null)
        {
            throw new KeyNotFoundException($"Этап с ID {stageId} не найден.");
        }

        return stage;
    }

    public async Task UpdateStageAsync(int stageId, UpdateStageRequest request, CancellationToken cancellationToken)
    {
        // Поиск сущности для обновления
        var stage = await _context.Stages
            .FirstOrDefaultAsync(s => s.StageId == stageId, cancellationToken);

        if (stage == null)
        {
            throw new KeyNotFoundException($"Этап с ID {stageId} не найден.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (request.ProgressPercent.HasValue)
        {
            stage.ProgressPercent = request.ProgressPercent.Value;

            // Автоматическая смена статуса при достижении 100%
            if (stage.ProgressPercent == 100)
            {
                stage.Status = StageStatus.Completed;
            }
            // Если прогресс > 0, но статус Pending, ставим InProgress
            else if (stage.ProgressPercent > 0 && stage.Status == StageStatus.Pending)
            {
                stage.Status = StageStatus.InProgress;
            }
            else if (stage.ProgressPercent < 100 && stage.Status == StageStatus.Completed)
            {
                stage.Status = StageStatus.InProgress;
            }
        }

        if (request.Status is not null)
        {
            if (!Enum.TryParse<StageStatus>(request.Status, true, out var stageStatusEnum))
            {
                throw new ArgumentException(
                    $"Статус '{request.Status}' не является корректным значением для StageStatus.");
            }

            // Нельзя вручную установить Completed, если прогресс < 100
            if (stageStatusEnum == StageStatus.Completed && stage.ProgressPercent < 100)
            {
                throw new ArgumentException("Нельзя установить статус 'Completed', если ProgressPercent меньше 100.");
            }

            stage.Status = stageStatusEnum;
        }

        // Обновление SpecialistUserId
        if (request.SpecialistUserId.HasValue)
        {
            // Проверка, что SpecialistUserId существует
            var userExists = await _context.Users
                .AnyAsync(u => u.UserId == request.SpecialistUserId.Value, cancellationToken);

            if (!userExists)
            {
                throw new ArgumentException($"Специалист с ID {request.SpecialistUserId.Value} не найден.");
            }

            stage.SpecialistUserId = request.SpecialistUserId.Value;
        }

        if (request.Deadline.HasValue)
        {
            stage.Deadline = request.Deadline.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProjectAsync(int stageId, CancellationToken cancellationToken)
    {
        if (stageId <= 0)
        {
            throw new ArgumentException("Идентификатор этапа должен быть положительным числом.", nameof(stageId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var stage = await _context.Stages
            .FirstOrDefaultAsync(s => s.StageId == stageId, cancellationToken);

        if (stage == null)
        {
            throw new KeyNotFoundException($"Этап с ID {stageId} не найден для удаления.");
        }

        _context.Stages.Remove(stage);
        await _context.SaveChangesAsync(cancellationToken);
    }
}