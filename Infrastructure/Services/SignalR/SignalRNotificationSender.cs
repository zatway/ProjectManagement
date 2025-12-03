using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;
using Application.Interfaces.SignalR;
using Infrastructure.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.SignalR;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(NotificationResponse notification)
    {
        Console.WriteLine($"[SignalRNotificationSender] Sending notification. NotificationId: {notification.NotificationId}, UserId: {notification.UserId}, Message: {notification.Message}");
        
        var groupName = notification.UserId.ToString();
        Console.WriteLine($"[SignalRNotificationSender] Sending to group: '{groupName}'");
        
        try
        {
            // Отправляем уведомление конкретному пользователю через группу
            // Пользователь был добавлен в группу с именем его UserId при подключении
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ReceiveNotification", notification);
            
            Console.WriteLine($"[SignalRNotificationSender] Notification sent successfully to group '{groupName}'");
            
            // Также отправляем всем подключенным клиентам как fallback (на случай, если группа не работает)
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
            Console.WriteLine($"[SignalRNotificationSender] Also sent to all clients as backup");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalRNotificationSender] ERROR sending notification: {ex.Message}");
            Console.WriteLine($"[SignalRNotificationSender] Stack trace: {ex.StackTrace}");
            
            // В случае ошибки пробуем отправить всем (fallback)
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                Console.WriteLine($"[SignalRNotificationSender] Fallback: sent to all clients");
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"[SignalRNotificationSender] ERROR in fallback: {fallbackEx.Message}");
            }
        }
    }
}