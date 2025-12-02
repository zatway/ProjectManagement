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
using Infrastructure.Services; 

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ProjectManagementDbContext _context;
    private readonly INotificationSender _notificationSender;

    public NotificationService(ProjectManagementDbContext context, INotificationSender notificationSender)
    {
        _context = context;
        _notificationSender = notificationSender;
    }

    /// <summary>
    /// Получает все уведомления для конкретного пользователя.
    /// </summary>
    public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // 1. Получаем уведомления, включая связанный проект для ProjectName
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
    public async Task CreateAndSendNotificationAsync(int userId, int projectId, string message, CancellationToken cancellationToken)
    {
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

        await _notificationSender.SendNotificationAsync(notificationDto);
    }
}