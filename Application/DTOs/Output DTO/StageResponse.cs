namespace Application.DTOs.Output_DTO;

public class StageResponse
{
    public string Name { get; set; }
    public string StageType { get; set; }
    public string ProgressPercent { get; set; }
    public string Status { get; set; }
    public DateTime Deadline { get; set; }
    public string SpecialistUserFullName { get; set; }
}