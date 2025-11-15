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
    /// Тип отчета (например, "PDF_Act", "Excel_KPI").
    /// </summary>
    public string ReportType { get; set; }

    /// <summary>
    /// Дата и время генерации отчета.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Путь к сохраненному файлу отчета.
    /// </summary>
    public string FilePath { get; set; }

    // Навигационные свойства для связи
    
    /// <summary>
    /// Проект, к которому относится отчет.
    /// </summary>
    public Project Project { get; set; }

    /// <summary>
    /// Этап, к которому относится отчет.
    /// </summary>
    public Stage Stage { get; set; }
}