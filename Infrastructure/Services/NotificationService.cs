using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Application.Interfaces.SignalR;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using Application.DTOs.Output_DTO.SignalR;
using Infrastructure.Contexts;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ProjectManagementDbContext _context;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ProjectManagementDbContext context,
        INotificationSender notificationSender,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает все уведомления (прочитанные и непрочитанные) для указанного пользователя.
    /// </summary>
    public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                ProjectId = n.ProjectId,
                ProjectName = n.Project.Name,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
    
    /// <summary>
    /// Отмечает уведомление как прочитанное для указанного пользователя.
    /// </summary>
    /// <param name="notificationId">Идентификатор уведомления.</param>
    /// <param name="userId">Идентификатор пользователя‑владельца уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);

        if (notification == null)
        {
            throw new KeyNotFoundException($"Уведомление с ID {notificationId} не найдено.");
        }
        
        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException("У пользователя нет прав для изменения этого уведомления.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Удаляет уведомление пользователя.
    /// </summary>
    /// <param name="notificationId">Идентификатор уведомления.</param>
    /// <param name="userId">Идентификатор пользователя‑владельца уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task DeleteNotificationAsync(int notificationId, int userId, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);

        if (notification == null)
        {
            return;
        }

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException("У пользователя нет прав для удаления этого уведомления.");
        }
        
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Создает уведомление в базе данных и отправляет его через SignalR.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя‑получателя уведомления.</param>
    /// <param name="projectId">Идентификатор связанного проекта.</param>
    /// <param name="message">Текст уведомления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task CreateAndSendNotificationAsync(int userId, int projectId, string message, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Creating notification. UserId: {UserId}, ProjectId: {ProjectId}, Message: {Message}",
            userId,
            projectId,
            message);

        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var notification = new Notification
        {
            UserId = userId,
            ProjectId = projectId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification saved to DB. NotificationId: {NotificationId}, UserId: {UserId}, ProjectId: {ProjectId}",
            notification.NotificationId,
            notification.UserId,
            notification.ProjectId);
        
        var notificationDto = new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            ProjectId = notification.ProjectId,
            ProjectName = project.Name,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        _logger.LogDebug("Sending notification {NotificationId} to user {UserId} via SignalR", notification.NotificationId, notification.UserId);
        await _notificationSender.SendNotificationAsync(notificationDto);
        _logger.LogInformation("Notification {NotificationId} sent to user {UserId}", notification.NotificationId, notification.UserId);
    }
}