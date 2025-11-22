namespace Application.DTOs.Output_DTO;

public class ShortReportResponse
{
    public int ReportId { get; set; }
    
    public string ProjectName { get; set; } = null!;
    
    public string ReportType { get; set; } = null!;
    
    public string Status { get; set; } = null!;
    
    public DateTime GeneratedAt { get; set; }
    
    public string? TargetFileName { get; set; }
}