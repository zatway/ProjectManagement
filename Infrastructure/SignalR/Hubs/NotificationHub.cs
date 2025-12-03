using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Infrastructure.SignalR.Hubs;

[Authorize] 
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Вызывается при установлении соединения клиентом.
    /// Добавляет пользователя в группу с его идентификатором для адресной отправки уведомлений.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false;
        var claimsCount = Context.User?.Claims?.Count() ?? 0;

        _logger.LogDebug(
            "SignalR client connecting. ConnectionId: {ConnectionId}, Authenticated: {IsAuthenticated}, ClaimsCount: {ClaimsCount}",
            Context.ConnectionId,
            isAuthenticated,
            claimsCount);

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;

        _logger.LogDebug("SignalR user id from claims: {UserId}", userId ?? "null");
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            _logger.LogInformation(
                "User {UserId} added to SignalR group. ConnectionId: {ConnectionId}",
                userId,
                Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning(
                "SignalR connection established without user id. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();

        _logger.LogInformation(
            "SignalR connection established successfully. ConnectionId: {ConnectionId}",
            Context.ConnectionId);
    }

    /// <summary>
    /// Вызывается при разрыве соединения клиентом.
    /// Удаляет пользователя из его группы.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

            _logger.LogInformation(
                "User {UserId} removed from SignalR group on disconnect. ConnectionId: {ConnectionId}",
                userId,
                Context.ConnectionId);
        }

        if (exception is not null)
        {
            _logger.LogWarning(
                exception,
                "SignalR connection disconnected with error. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

