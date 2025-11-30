using Application.DTOs.Output_DTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUsersService
{
    Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken);
}