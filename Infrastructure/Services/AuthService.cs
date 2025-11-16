using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Сервис для авторизации в системе. Определяет интерфейс IAuthService
/// </summary>
public class AuthService : IAuthService
{
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}