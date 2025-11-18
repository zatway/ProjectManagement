namespace Application.DTOs.Input_DTO;

public class RefreshTokenRequest
{
    public string Token { get; set; } // Истекший JWT
    public string RefreshToken { get; set; } // Действующий Refresh Token
}