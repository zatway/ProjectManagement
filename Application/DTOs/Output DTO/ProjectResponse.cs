namespace Application.DTOs.Output_DTO;

public class ProjectResponse
{
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } // Отправляем статус как строку
    public int StagesCount { get; set; }
}