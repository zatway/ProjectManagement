using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using BCrypt.Net;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Сервис для авторизации в системе. Определяет интерфейс IAuthService
/// </summary>
public class AuthService : IAuthService
{
    private readonly ProjectManagementDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(ProjectManagementDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Регистрация нового пользователя с хешированием пароля.
    /// </summary>
    public async Task Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exists = await _context.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);

        if (exists)
        {
            throw new ArgumentException("Пользователь с таким именем уже существует.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, SaltRevision.Revision2A);

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash, // Сохраняем хеш
            Role = request.Role, // Сохраняем роль как строку
            FullName = request.FullName
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Аутентификация пользователя и выдача токена.
    /// </summary>
    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.UserName, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Неверное имя пользователя или пароль.");
        }

        // 💡 2. Верификация пароля с использованием BCrypt
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Неверное имя пользователя или пароль.");
        }

        // 💡 3. Генерация JWT-токена
        var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Role.ToString());
        
        // В реальном проекте здесь генерируется RefreshToken
        
        return new LoginResponse
        {
            Id = user.UserId,
            Token = token,
            RefreshToken = "not_implemented" // Пока заглушка
        };
    }
}