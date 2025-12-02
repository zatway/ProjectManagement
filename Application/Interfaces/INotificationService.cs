using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;

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

    /// <summary>
    /// Создает уведомление в базе данных и отправляет его через SignalR.
    /// </summary>
    Task CreateAndSendNotificationAsync(int userId, int projectId, string message, CancellationToken cancellationToken);
}