using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Linq;

namespace Infrastructure.SignalR.Hubs;

[Authorize] 
public class NotificationHub : Hub
{
    // Метод, который вызывается при установлении соединения клиентом
    public override async Task OnConnectedAsync()
    {
        // Логирование для отладки
        Console.WriteLine($"[SignalR] Client connecting. ConnectionId: {Context.ConnectionId}");
        Console.WriteLine($"[SignalR] User authenticated: {Context.User?.Identity?.IsAuthenticated ?? false}");
        Console.WriteLine($"[SignalR] User claims count: {Context.User?.Claims?.Count() ?? 0}");
        
        // 1. Получаем ID пользователя из Claims (используем тот же ClaimTypes.NameIdentifier)
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
        Console.WriteLine($"[SignalR] UserId from claims: {userId ?? "null"}");
        
        if (!string.IsNullOrEmpty(userId))
        {
            // 2. Добавляем пользователя в группу, названную по его ID. 
            // Это позволяет нам отправлять сообщения конкретному пользователю, 
            // даже если у него несколько открытых вкладок.
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"[SignalR] User {userId} added to group. ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[SignalR] WARNING: UserId is null or empty. Connection may fail.");
        }
        
        await base.OnConnectedAsync();
        Console.WriteLine($"[SignalR] Connection established successfully. ConnectionId: {Context.ConnectionId}");
    }

    // Метод, который вызывается при разрыве соединения
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
        
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

