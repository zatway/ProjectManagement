using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Представляет сущность уведомления для реал-тайм системы SignalR.
/// </summary>
public class Notification
{
    /// <summary>
    /// Уникальный идентификатор уведомления (Primary Key).
    /// </summary>
    [Key]
    public int NotificationId { get; set; }

    /// <summary>
    /// Идентификатор пользователя-получателя (Foreign Key).
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор проекта, связанного с уведомлением (может быть null).
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Текст уведомления (например, "Дедлайн по этапу X приближается").
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Message { get; set; } = String.Empty;

    /// <summary>
    /// Статус прочтения уведомления.
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Дата и время создания уведомления.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Навигационные свойства для связи
    
    /// <summary>
    /// Пользователь, которому адресовано уведомление.
    /// </summary>
    public User User { get; set; }
    
    /// <summary>
    /// Проект, с которым связано уведомление.
    /// </summary>
    public Project Project { get; set; }
}