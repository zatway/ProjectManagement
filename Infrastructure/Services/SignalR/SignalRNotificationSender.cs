using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Application.DTOs.Output_DTO.SignalR;
using Application.Interfaces.SignalR;
using Infrastructure.SignalR.Hubs;

namespace Infrastructure.Services.SignalR;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHubStub> _hubContext;

    public SignalRNotificationSender(IHubContext<NotificationHubStub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task SendReportCompleteNotificationAsync(int userId, NotificationResponse notification)
    {
        // Клиентский метод: "ReportCompleted"
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("ReportCompleted", notification);
    }

    public async Task SendNewSystemNotificationAsync(int userId, NotificationResponse notification)
    {
        // Клиентский метод: "NewSystemNotification"
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("NewSystemNotification", notification);
    }

    public async Task SendStageStatusUpdateAsync(int userId, NotificationResponse notification)
    {
        // Клиентский метод: "StageStatusUpdated"
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("StageStatusUpdated", notification);
    }
}