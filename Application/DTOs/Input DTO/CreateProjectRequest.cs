using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Input_DTO;

/// <summary>
/// DTO для создания нового проекта. Используется для входящих запросов POST.
/// Содержит атрибуты валидации.
/// </summary>
public class CreateProjectRequest
{
    [Required(ErrorMessage = "Название проекта обязательно.")]
    [StringLength(255, ErrorMessage = "Название не может быть длиннее 255 символов.")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    

    [Required(ErrorMessage = "Бюджет проекта обязателен.")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Бюджет должен быть больше нуля.")]
    public decimal Budget { get; set; }

    [Required(ErrorMessage = "Дата начала обязательна.")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "Дата завершения обязательна.")]
    public DateTime EndDate { get; set; }
}