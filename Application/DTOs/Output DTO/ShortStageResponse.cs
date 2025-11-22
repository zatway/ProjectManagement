namespace Application.DTOs.Output_DTO;

public class ShortStageResponse
{
    public int StageId { get; set; }
    public string Name { get; set; }
    public string StageType { get; set; }
    public float ProgressPercent { get; set; }
    public string Status { get; set; }
}