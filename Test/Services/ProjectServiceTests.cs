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
/// Юнит-тесты для ProjectService - критичные функции управления проектами.
/// </summary>
[TestFixture]
public class ProjectServiceTests
{
    private ProjectManagementDbContext _context;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<ILogger<ProjectService>> _mockLogger;
    private ProjectService _projectService;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<ProjectService>>();
        _projectService = new ProjectService(_context, _mockNotificationService.Object, _mockLogger.Object);

        // Создаем тестового пользователя
        _testUser = new User
        {
            Username = "testuser",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Administrator
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Тест успешного создания проекта.
    /// </summary>
    [Test]
    public async Task CreateProjectAsync_ValidRequest()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Test Project",
            Description = "Test Description",
            Budget = 10000m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var projectId = await _projectService.CreateProjectAsync(request, _testUser.UserId, CancellationToken.None);

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
        Assert.That(project, Is.Not.Null);
        Assert.That(project.Name, Is.EqualTo("Test Project"));
        Assert.That(project.Description, Is.EqualTo("Test Description"));
        Assert.That(project.Budget, Is.EqualTo(10000m));
        Assert.That(project.Status, Is.EqualTo(ProjectStatus.Planning));
        Assert.That(project.CreatedByUserId, Is.EqualTo(_testUser.UserId));
    }

    /// <summary>
    /// Тест создания проекта с датой завершения раньше даты начала
    /// </summary>
    [Test]
    public async Task CreateProjectAsync_EndDateBeforeStartDate()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Test Project",
            Description = "Test Description",
            Budget = 10000m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-10)
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _projectService.CreateProjectAsync(request, _testUser.UserId, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не может быть раньше"));
    }

    /// <summary>
    /// Тест успешного обновления проекта.
    /// </summary>
    [Test]
    public async Task UpdateProjectAsync_ValidRequest()
    {
        // Arrange
        var project = new Project
        {
            Name = "Original Name",
            Description = "Original Description",
            Budget = 5000m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = ProjectStatus.Planning,
            CreatedByUserId = _testUser.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateProjectRequest
        {
            Name = "Updated Name",
            Budget = 15000m,
            Status = "Active"
        };

        await _projectService.UpdateProjectAsync(project.ProjectId, updateRequest, CancellationToken.None);

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == project.ProjectId);
        Assert.That(updatedProject, Is.Not.Null);
        Assert.That(updatedProject.Name, Is.EqualTo("Updated Name"));
        Assert.That(updatedProject.Budget, Is.EqualTo(15000m));
        Assert.That(updatedProject.Status, Is.EqualTo(ProjectStatus.Active));
    }

    /// <summary>
    /// Тест обновления дат проекта - проверка конвертации в UTC.
    /// </summary>
    [Test]
    public async Task UpdateProjectAsync_UpdateDatesc()
    {
        var project = new Project
        {
            Name = "Test Project",
            Description = "Description",
            Budget = 10000m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = ProjectStatus.Planning,
            CreatedByUserId = _testUser.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var newStartDate = new DateTime(2025, 12, 30, 19, 0, 0, DateTimeKind.Unspecified);
        var newEndDate = new DateTime(2026, 2, 27, 19, 0, 0, DateTimeKind.Unspecified);

        var updateRequest = new UpdateProjectRequest
        {
            StartDate = newStartDate,
            EndDate = newEndDate,
            Status = null
        };

        await _projectService.UpdateProjectAsync(project.ProjectId, updateRequest, CancellationToken.None);

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == project.ProjectId);
        Assert.That(updatedProject, Is.Not.Null);
        Assert.That(updatedProject.StartDate.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(updatedProject.EndDate.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(updatedProject.StartDate.Year, Is.EqualTo(2025));
        Assert.That(updatedProject.EndDate.Year, Is.EqualTo(2026));
    }

    /// <summary>
    /// Тест обновления несуществующего проекта
    /// </summary>
    [Test]
    public async Task UpdateProjectAsync_NonExistentProject()
    {
        // Arrange
        var updateRequest = new UpdateProjectRequest
        {
            Name = "Updated Name"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _projectService.UpdateProjectAsync(99999, updateRequest, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не найден"));
    }

    /// <summary>
    /// Тест получения проекта по ID.
    /// </summary>
    [Test]
    public async Task GetProjectByIdAsync_ValidId()
    {
        var project = new Project
        {
            Name = "Test Project",
            Description = "Test Description",
            Budget = 10000m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = ProjectStatus.Planning,
            CreatedByUserId = _testUser.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _projectService.GetProjectByIdAsync(project.ProjectId, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ProjectId, Is.EqualTo(project.ProjectId));
        Assert.That(result.Name, Is.EqualTo("Test Project"));
        Assert.That(result.Description, Is.EqualTo("Test Description"));
        Assert.That(result.Budget, Is.EqualTo(10000m));
        Assert.That(result.StartDate, Is.EqualTo(project.StartDate));
        Assert.That(result.EndDate, Is.EqualTo(project.EndDate));
    }
}

