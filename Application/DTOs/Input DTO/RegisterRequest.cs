using Domain.Enums;

namespace Application.DTOs.Input_DTO;

public class RegisterRequest
{
    public string Username { get; set; }
    public UserRole Role { get; set; }
    public string FullName { get; set; }
}