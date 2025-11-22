using Application.DTOs.Output_DTO;

namespace Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Отмечает уведомление как прочитанное (IsRead = true).
    /// </summary>
    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет уведомление по его ID.
    /// </summary>
    Task DeleteNotificationAsync(int notificationId, int userId, CancellationToken cancellationToken);
}