namespace DefaultNamespace;

public enum ProjectStatus
{
    /// <summary>
    /// Проект находится на стадии планирования
    /// </summary>
    Planning,
    
    /// <summary>
    /// Проект активно выполняется
    /// </summary>
    Active,
    
    /// <summary>
    /// Выполнение проекта временно приостановлено
    /// </summary>
    OnHold,
    
    /// <summary>
    /// Все работы по проекту завершены
    /// </summary>
    Completed,
    
    /// <summary>
    /// Проект отменен и не будет продолжен
    /// </summary>
    Canceled
}