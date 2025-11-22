namespace Application.DTOs.Output_DTO;

public class StageResponse
{
    public int StageId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string Name { get; set; }
    public string StageType { get; set; }
    public float ProgressPercent { get; set; }
    public string Status { get; set; }
    public DateTime Deadline { get; set; }
    public string SpecialistUserFullName { get; set; }
}