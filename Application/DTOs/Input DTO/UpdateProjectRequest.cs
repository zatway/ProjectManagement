using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Input_DTO;

/// <summary>
/// DTO для обновления проекта. Используется для входящих запросов PATCH
/// </summary>
public class UpdateProjectRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }
    
    public string? Status { get; set; } = string.Empty;
    
    public decimal? Budget { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}