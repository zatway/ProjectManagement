namespace Domain.Enums;

public enum StageStatus
{
    /// <summary>
    /// Ожидает начала выполнения
    /// </summary>
    Pending,
    
    /// <summary>
    /// Находится в процессе выполнения
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Выполнение этапа завершено
    /// </summary>
    Completed,
    
    /// <summary>
    /// Срок выполнения просрочен
    /// </summary>
    Delayed
}