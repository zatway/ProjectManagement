using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;
using Application.Interfaces.SignalR;
using Infrastructure.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.SignalR;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationSender> _logger;

    public SignalRNotificationSender(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationSender> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Отправляет уведомление в хаб SignalR для конкретного пользователя.
    /// </summary>
    /// <param name="notification">DTO уведомления для клиента.</param>
    public async Task SendNotificationAsync(NotificationResponse notification)
    {
        _logger.LogDebug(
            "Sending notification via SignalR. NotificationId: {NotificationId}, UserId: {UserId}",
            notification.NotificationId,
            notification.UserId);

        var groupName = notification.UserId.ToString();

        try
        {
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation(
                "Notification {NotificationId} sent to group '{GroupName}'",
                notification.NotificationId,
                groupName);
        }
        catch (Exception ex)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(
                    fallbackEx,
                    "Error in fallback while sending notification {NotificationId} to all clients",
                    notification.NotificationId);
            }
        }
    }
}