/// <summary>
/// Представляет сущность этапа, связанного с конкретным проектом.
/// </summary>
public class Stage
{
    /// <summary>
    /// Уникальный идентификатор этапа (Primary Key).
    /// </summary>
    public int StageId { get; set; }

    /// <summary>
    /// Идентификатор проекта, к которому относится этап (Foreign Key).
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Название этапа.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Тип этапа (например, "Exploration", "Design", "Installation", "Testing").
    /// </summary>
    public string StageType { get; set; }

    /// <summary>
    /// Крайний срок выполнения этапа (дедлайн).
    /// </summary>
    public DateTime Deadline { get; set; }

    /// <summary>
    /// Процент выполнения этапа (от 0 до 100).
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Статус выполнения этапа (например, "In Progress", "Completed", "Delayed").
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Идентификатор пользователя (Специалиста), ответственного за этап (Foreign Key).
    /// </summary>
    public int SpecialistUserId { get; set; }

    // Навигационные свойства для связи
    
    /// <summary>
    /// Проект, к которому относится этап.
    /// </summary>
    public Project Project { get; set; }

    /// <summary>
    /// Пользователь (Специалист), ответственный за этап.
    /// </summary>
    public User Specialist { get; set; }

    /// <summary>
    /// Коллекция отчетов, связанных с этим этапом.
    /// </summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}