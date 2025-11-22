namespace Application.DTOs.Output_DTO;

/// <summary>
/// Метаданные отчета, возвращаемые пользователю после запуска генерации.
/// </summary>
public class ReportResponse
{
    public int ReportId { get; set; }
    public int ProjectId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; 
    public DateTime GeneratedAt { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}