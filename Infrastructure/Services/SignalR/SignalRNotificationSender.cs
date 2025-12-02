using Application.DTOs.Output_DTO;
using Application.DTOs.Output_DTO.SignalR;
using Application.Interfaces.SignalR;
using Infrastructure.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.SignalR;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHubStub> _hubContext;

    public SignalRNotificationSender(IHubContext<NotificationHubStub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(NotificationResponse notification)
    {
        await _hubContext.Clients
            .All
            .SendAsync("ReceiveNotification", notification);
    }
}