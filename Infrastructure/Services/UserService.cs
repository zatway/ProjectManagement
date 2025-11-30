using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService : IUsersService
{
    private readonly ProjectManagementDbContext _context;

    public UserService(ProjectManagementDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync( CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new UserResponse
            {
                Id = u.UserId,
                FullName = u.FullName,
            }).
            ToListAsync(cancellationToken);
        return users;
    }
}