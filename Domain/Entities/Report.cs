using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет сущность сгенерированного отчета (PDF, Excel).
/// </summary>
public class Report
{
    /// <summary>
    /// Уникальный идентификатор отчета (Primary Key).
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// Идентификатор проекта, к которому относится отчет (Foreign Key).
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Идентификатор этапа, к которому относится отчет (может быть null для общих отчетов).
    /// </summary>
    public int? StageId { get; set; }

    /// <summary>
    /// Тип отчета
    /// </summary>
    public ReportType ReportType { get; set; }
    
    /// <summary>
    /// Статус готовности отчета
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Дата и время генерации отчета.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
    
    /// <summary>
    /// Идентификатор пользователя, сгенерировавшего отчет (Foreign Key)
    /// </summary>
    public int GeneratedByUserId { get; set; }

    /// <summary>
    /// Путь к сохраненному файлу отчета.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// JSON-конфигурация, отправленная с фронтенда (например, список полей для включения).
    /// </summary>
    public string? ReportConfig { get; set; }
    
    /// <summary>
    /// Имя файла
    /// </summary>
    public string? TargetFileName { get; set; }
    
    // Навигационные свойства
    public Project Project { get; set; } = null!;
    public Stage? Stage { get; set; }
    public User GeneratedBy { get; set; } = null!;
}