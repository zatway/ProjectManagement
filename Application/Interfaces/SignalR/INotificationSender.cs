using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;

namespace Application.Interfaces.SignalR;

public interface INotificationSender
{
    /// <summary>
    /// Топик: Уведомляет всех пользователей о уведомлении
    /// </summary>
    Task SendNotificationAsync(NotificationResponse notification);
}