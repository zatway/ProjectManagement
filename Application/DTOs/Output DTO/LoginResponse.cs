namespace Application.DTOs.Output_DTO;

public class LoginResponse
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}