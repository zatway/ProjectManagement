using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using BCrypt.Net;
using Domain.Entities;
using Infrastructure.Contexts;
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

        // 💡Верификация пароля с использованием BCrypt
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Неверное имя пользователя или пароль.");
        }

        //Генерация и сохранение Refresh Token
        var refreshToken = GenerateRefreshToken();
        
        user.RefreshToken = refreshToken;
        // Срок действия - 7 дней
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 

        // Сохранение изменений в БД
        // EF Core отслеживает user и сгенерирует UPDATE-запрос только для двух полей: RefreshToken и RefreshTokenExpiryTime
        await _context.SaveChangesAsync(cancellationToken);
        
        var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Role.ToString());
        
        return new LoginResponse
        {
            Id = user.UserId,
            Token = token,
            RefreshToken = refreshToken
        };
    }
    
    /// <summary>
    /// Обновляет пару токенов, используя Refresh Token.
    /// </summary>
    public async Task<LoginResponse> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Поиск пользователя по Refresh Token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null)
        {
            // Refresh Token не найден в БД
            throw new UnauthorizedAccessException("Недействительный или отозванный Refresh Token.");
        }

        // Проверка срока действия Refresh Token
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            // Срок действия Refresh Token истек
            throw new UnauthorizedAccessException("Срок действия Refresh Token истек. Требуется повторный вход.");
        }
        
        // Генерация новой пары токенов
        var newJwtToken = _jwtService.GenerateToken(user.UserId, user.Username, user.Role.ToString());
        var newRefreshToken = GenerateRefreshToken(); // Новый Refresh Token

        // Обновление и сохранение в БД (отзыв старого токена)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync(cancellationToken);

        // Возврат новой пары токенов
        return new LoginResponse
        {
            Id = user.UserId,
            Token = newJwtToken,
            RefreshToken = newRefreshToken
        };
    }
    
    private string GenerateRefreshToken()
    {
        // 💡 Используем безопасный генератор случайных чисел
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}