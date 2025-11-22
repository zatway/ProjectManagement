using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using Infrastructure.Contexts;
using Infrastructure.Services; 

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ProjectManagementDbContext _context;

    public NotificationService(ProjectManagementDbContext context)
    {
        _context = context;
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
}