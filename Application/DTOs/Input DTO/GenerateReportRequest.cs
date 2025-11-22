using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Input_DTO;

/// <summary>
/// DTO для запроса на генерацию отчета с возможностью настройки полей.
/// </summary>
public class GenerateReportRequest
{
    [Required(ErrorMessage = "ID проекта обязателен.")]
    public int ProjectId { get; set; }
    
    public int? StageId { get; set; }
    
    [Required(ErrorMessage = "Тип отчета обязателен.")]
    public string ReportType { get; set; } = string.Empty; 
    
    /// <summary>
    /// JSON-конфигурация (например, {"IncludeDeadline": true, "Fields": ["Name", "Progress"]}).
    /// </summary>
    public string? ReportConfig { get; set; }
    
    /// <summary>
    /// Имя файла
    /// </summary>
    public string? TargetFileName { get; set; }
}