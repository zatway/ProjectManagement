using Application.DTOs.Input_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Services;

/// <summary>
/// Юнит-тесты для AuthService - критичная функция авторизации и регистрации.
/// </summary>
[TestFixture]
public class AuthServiceTests
{
    private ProjectManagementDbContext _context;
    private Mock<IJwtService> _mockJwtService;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _mockJwtService = new Mock<IJwtService>();
        _authService = new AuthService(_context, _mockJwtService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Тест успешной регистрации нового пользователя.
    /// </summary>
    [Test]
    public async Task Register_ValidRequest()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "TestPassword123!",
            FullName = "Test User",
            Role = "Specialist"
        };

        await _authService.Register(request, CancellationToken.None);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.FullName, Is.EqualTo("Test User"));
        Assert.That(user.Role, Is.EqualTo(UserRole.Specialist));
        Assert.That(user.PasswordHash, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Тест регистрации с существующим именем пользователя
    /// </summary>
    [Test]
    public async Task Register_DuplicateUsername()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser",
            PasswordHash = "hash",
            FullName = "Existing User",
            Role = UserRole.Specialist
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "existinguser",
            Password = "TestPassword123!",
            FullName = "Test User",
            Role = "Specialist"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.Register(request, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("уже существует"));
    }
    
    [Test]
    public async Task Register_InvalidRole()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "TestPassword123!",
            FullName = "Test User",
            Role = "InvalidRole"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.Register(request, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не является корректным значением"));
    }

    /// <summary>
    /// Тест успешного входа пользователя.
    /// </summary>
    [Test]
    public async Task Login_ValidCredentials()
    {
        // Arrange
        var password = "TestPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.SaltRevision.Revision2A);
        
        var user = new User
        {
            Username = "testuser",
            PasswordHash = passwordHash,
            FullName = "Test User",
            Role = UserRole.Specialist
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockJwtService.Setup(x => x.GenerateToken(
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .Returns("mock-jwt-token");

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = password
        };

        var result = await _authService.Login(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.EqualTo("mock-jwt-token"));
        Assert.That(result.Id, Is.EqualTo(user.UserId));
        Assert.That(result.FullName, Is.EqualTo("Test User"));
        Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Тест входа с неверным паролем
    /// </summary>
    [Test]
    public async Task Login_InvalidPassword()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword", BCrypt.Net.SaltRevision.Revision2A);
        
        var user = new User
        {
            Username = "testuser",
            PasswordHash = passwordHash,
            FullName = "Test User",
            Role = UserRole.Specialist
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _authService.Login(request, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("Неверное имя пользователя или пароль"));
    }

    /// <summary>
    /// Тест входа с несуществующим пользователем
    /// </summary>
    [Test]
    public async Task Login_NonExistentUser()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "nonexistent",
            Password = "SomePassword"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _authService.Login(request, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("Неверное имя пользователя или пароль"));
    }
}

