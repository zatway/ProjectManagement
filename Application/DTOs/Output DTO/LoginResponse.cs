namespace Application.DTOs.Output_DTO;

public class LoginResponse
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string FullName { get; set; }
    public string UserRole { get; set; }
}