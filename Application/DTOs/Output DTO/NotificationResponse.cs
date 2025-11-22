using System;

namespace Application.DTOs.Output_DTO;

/// <summary>
/// Полная модель уведомления для передачи на клиент.
/// </summary>
public class NotificationResponse
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public int? ProjectId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProjectName { get; set; }
}