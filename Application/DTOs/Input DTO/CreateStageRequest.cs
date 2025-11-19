using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Input_DTO;

public class CreateStageRequest
{
    [Required(ErrorMessage = "Название этапа обязательно.")]
    [StringLength(255, ErrorMessage = "Название не может быть длиннее 255 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Тип этапа обязателен.")]
    public string StageType { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Процент выполнения этапа обязателен.")]
    [Range(0, 100, ErrorMessage = "Прцоент не должен быть больше 100 и меньше 0")]
    public float ProgressPercent { get; set; }

    [Required(ErrorMessage = "Дата дедлайна обязательна.")]
    public DateTime Deadline { get; set; }
    
    [Required(ErrorMessage = "Статус проекта обязателен")]
    public string Status { get; set; } 
    
    [Required(ErrorMessage = "Идентификатор пользователя, ответственного за этап обязателен")]
    public int SpecialistUserId { get; set; }
}