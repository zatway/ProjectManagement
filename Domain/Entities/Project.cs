/// <summary>
/// Представляет сущность проекта, содержащую основные данные.
/// </summary>
public class Project
{
    /// <summary>
    /// Уникальный идентификатор проекта (Primary Key).
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Название проекта. Должно быть уникальным.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Подробное описание проекта.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Бюджет проекта. Требуется валидация: должно быть больше нуля.
    /// </summary>
    public decimal Budget { get; set; }

    /// <summary>
    /// Планируемая дата начала проекта (YYYY-MM-DD).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Планируемая дата завершения проекта (YYYY-MM-DD).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Текущий статус проекта (например, "Active", "Completed").
    /// </summary>
    public ProjectStatus Status { get; set; }
    /// <summary>
    /// Идентификатор пользователя, создавшего этот проект (Foreign Key).
    /// </summary>
    public int CreatedByUserId { get; set; }
    
    /// <summary>
    /// Дата и время создания записи проекта.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Навигационные свойства для связи
    
    /// <summary>
    /// Пользователь, создавший проект (Навигационное свойство).
    /// </summary>
    public User CreatedBy { get; set; }

    /// <summary>
    /// Коллекция этапов, входящих в данный проект.
    /// </summary>
    public ICollection<Stage> Stages { get; set; } = new List<Stage>();

    /// <summary>
    /// Коллекция отчетов, связанных с этим проектом.
    /// </summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}