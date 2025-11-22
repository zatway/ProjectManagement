namespace Domain.Enums;

/// <summary>
/// Статус процесса генерации отчета.
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Генерация запрошена и ожидает обработки.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Отчет находится в процессе генерации.
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// Генерация успешно завершена, файл доступен для скачивания.
    /// </summary>
    Complete = 2,
    
    /// <summary>
    /// Генерация завершилась с ошибкой.
    /// </summary>
    Failed = 3
}