using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Application.Interfaces.SignalR;

namespace Api.Hubs;

[Authorize] 
public class NotificationHub : Hub
{
    // Метод, который вызывается при установлении соединения клиентом
    public override async Task OnConnectedAsync()
    {
        // 1. Получаем ID пользователя из Claims (используем тот же ClaimTypes.NameIdentifier)
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // 2. Добавляем пользователя в группу, названную по его ID. 
            // Это позволяет нам отправлять сообщения конкретному пользователю, 
            // даже если у него несколько открытых вкладок.
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        
        await base.OnConnectedAsync();
    }

    // Метод, который вызывается при разрыве соединения
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Убираем пользователя из группы при отключении
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // 💡 Примечание: Мы не добавляем здесь методы, которые клиент может вызвать.
    // Наш хаб используется только для маршрутизации сообщений Server -> Client.
}