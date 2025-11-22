using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;

namespace Application.Interfaces.SignalR;

public interface INotificationSender
{
    /// <summary>
    /// Топик: Уведомляет пользователя о завершении генерации отчета.
    /// </summary>
    Task SendReportCompleteNotificationAsync(int userId, NotificationResponse notification);

    /// <summary>
    /// Топик: Уведомляет пользователя о новом системном событии или сообщении.
    /// </summary>
    Task SendNewSystemNotificationAsync(int userId, NotificationResponse notification);

    /// <summary>
    /// Топик: Уведомляет всех пользователей, связанных с проектом, о смене статуса этапа.
    /// </summary>
    Task SendStageStatusUpdateAsync(int userId, NotificationResponse notification);
}