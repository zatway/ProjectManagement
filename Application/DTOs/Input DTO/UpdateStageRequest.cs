using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Input_DTO;

public class UpdateStageRequest
{
    [Required(ErrorMessage = "Процент выполнения этапа обязателен.")]
    [Range(0, 100, ErrorMessage = "Прцоент не должен быть больше 100 и меньше 0")]
    public float? ProgressPercent { get; set; }

    [Required(ErrorMessage = "Дата дедлайна обязательна.")]
    public DateTime? Deadline { get; set; }
    
    [Required(ErrorMessage = "Статус проекта обязателен")]
    public string? Status { get; set; } 
    
    [Required(ErrorMessage = "Идентификатор пользователя, ответственного за этап обязателен")]
    public int? SpecialistUserId { get; set; }
}