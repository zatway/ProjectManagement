namespace Application.DTOs.Output_DTO;

public class ShortProjectResponse
{
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; }
}