using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет сущность этапа, связанного с конкретным проектом.
/// </summary>
public class Stage
{
    /// <summary>
    /// Уникальный идентификатор этапа
    /// </summary>
    public int StageId { get; set; }

    /// <summary>
    /// Идентификатор проекта, к которому относится этап
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Название этапа
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Тип этапа
    /// </summary>
    public StageType StageType { get; set; }

    /// <summary>
    /// Крайний срок выполнения этапа (дедлайн)
    /// </summary>
    public DateTime Deadline { get; set; }

    /// <summary>
    /// Процент выполнения этапа (от 0 до 100)
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Статус выполнения этапа
    /// </summary>
    public StageStatus Status { get; set; }

    /// <summary>
    /// Идентификатор пользователя, ответственного за этап
    /// </summary>
    public int SpecialistUserId { get; set; }
    
    /// <summary>
    /// Проект, к которому относится этап
    /// </summary>
    public Project Project { get; set; }

    /// <summary>
    /// Пользователь (Специалист), ответственный за этап
    /// </summary>
    public User Specialist { get; set; }

    /// <summary>
    /// Коллекция отчетов, связанных с этим этапом
    /// </summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}