using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет сущность пользователя системы.
/// Используется для аутентификации и авторизации (Администратор/Специалист).
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя (Primary Key).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Логин пользователя для входа. Должен быть уникальным.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Хэш пароля пользователя.
    /// </summary>
    public string PasswordHash { get; set; }
    
    /// <summary>
    /// Поля для Refresh Token
    /// </summary>
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// Роль пользователя (например, "Administrator" или "Specialist").
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Полное имя пользователя.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Идентификатор последнего активного проекта, с которым работал пользователь.
    /// Позволяет восстанавливать состояние приложения.
    /// </summary>
    public int? LastActiveProjectId { get; set; }

    // Навигационные свойства для связи (один-ко-многим)
    
    /// <summary>
    /// Коллекция проектов, созданных этим пользователем.
    /// </summary>
    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();

    /// <summary>
    /// Коллекция этапов, за которые ответственен этот пользователь.
    /// </summary>
    public ICollection<Stage> AssignedStages { get; set; } = new List<Stage>();

    /// <summary>
    /// Коллекция уведомлений, предназначенных для этого пользователя.
    /// </summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    
    /// <summary>
    /// Коллекция отчетов, сгенерированных этим пользователем. 💡 ДОБАВЛЕНО
    /// </summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}