using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;

namespace Application.Interfaces;

public interface IAuthService
{
    // CRUD-операции
    Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken);

    Task Register(RegisterRequest request, CancellationToken cancellationToken);
}