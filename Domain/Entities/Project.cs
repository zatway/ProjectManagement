using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет сущность проекта, содержащую основные данные.
/// </summary>
public class Project
{
    /// <summary>
    /// Уникальный идентификатор проекта (Primary Key).
    /// </summary>
    [Key]
    public int ProjectId { get; set; }

    /// <summary>
    /// Название проекта. Должно быть уникальным.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = String.Empty;

    /// <summary>
    /// Подробное описание проекта.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Description { get; set; }  = String.Empty;

    /// <summary>
    /// Бюджет проекта.
    /// </summary>
    [Required]
    [Column(TypeName = "numeric(10, 2)")]
    public decimal Budget { get; set; }

    /// <summary>
    /// Планируемая дата начала проекта.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Планируемая дата завершения проекта.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Текущий статус проекта.
    /// </summary>
    public ProjectStatus Status { get; set; }
    
    /// <summary>
    /// Идентификатор пользователя, создавшего этот проект.
    /// </summary>
    public int CreatedByUserId { get; set; }
    
    /// <summary>
    /// Дата и время создания записи проекта.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Пользователь, создавший проект.
    /// </summary>
    public User CreatedBy { get; set; }

    /// <summary>
    /// Коллекция этапов, входящих в данный проект.
    /// </summary>
    public ICollection<Stage> Stages { get; set; } = new List<Stage>();

    /// <summary>
    /// Коллекция отчетов, связанных с этим проектом.
    /// </summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}