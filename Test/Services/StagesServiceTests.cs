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
/// Юнит-тесты для StagesService - критичные функции управления этапами проектов.
/// </summary>
[TestFixture]
public class StagesServiceTests
{
    private ProjectManagementDbContext _context;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<ILogger<StagesService>> _mockLogger;
    private StagesService _stagesService;
    private User _testUser;
    private Project _testProject;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<StagesService>>();
        _stagesService = new StagesService(_context, _mockNotificationService.Object, _mockLogger.Object);

        // Создаем тестового пользователя
        _testUser = new User
        {
            Username = "testuser",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Specialist
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        // Создаем тестовый проект
        _testProject = new Project
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
        _context.Projects.Add(_testProject);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Тест успешного создания этапа.
    /// </summary>
    [Test]
    public async Task CreateStageAsync_ValidRequest()
    {
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "Design",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 0,
            Status = "Pending",
            SpecialistUserId = _testUser.UserId
        };

        var stageId = await _stagesService.CreateStageAsync(request, _testProject.ProjectId, CancellationToken.None);

        var stage = await _context.Stages.FirstOrDefaultAsync(s => s.StageId == stageId);
        Assert.That(stage, Is.Not.Null);
        Assert.That(stage.Name, Is.EqualTo("Test Stage"));
        Assert.That(stage.ProjectId, Is.EqualTo(_testProject.ProjectId));
        Assert.That(stage.SpecialistUserId, Is.EqualTo(_testUser.UserId));
        Assert.That(stage.StageType, Is.EqualTo(StageType.Design));
        Assert.That(stage.Status, Is.EqualTo(StageStatus.Pending));
    } 

    /// <summary>
    /// Тест создания этапа с прогрессом 100%
    /// </summary>
    [Test]
    public async Task CreateStageAsync_Progress100Percent()
    {
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "Design",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 100,
            Status = "InProgress",
            SpecialistUserId = _testUser.UserId
        };

        var stageId = await _stagesService.CreateStageAsync(request, _testProject.ProjectId, CancellationToken.None);

        var stage = await _context.Stages.FirstOrDefaultAsync(s => s.StageId == stageId);
        Assert.That(stage, Is.Not.Null);
        Assert.That(stage.ProgressPercent, Is.EqualTo(100));
        Assert.That(stage.Status, Is.EqualTo(StageStatus.Completed));
    }

    /// <summary>
    /// Тест создания этапа с прогрессом > 0 и статусом Pending - статус должен автоматически стать InProgress.
    /// </summary>
    [Test]
    public async Task CreateStageAsync_ProgressGreaterThanZero()
    {
        // Arrange
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "Design",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 50,
            Status = "Pending",
            SpecialistUserId = _testUser.UserId
        };

        var stageId = await _stagesService.CreateStageAsync(request, _testProject.ProjectId, CancellationToken.None);

        var stage = await _context.Stages.FirstOrDefaultAsync(s => s.StageId == stageId);
        Assert.That(stage, Is.Not.Null);
        Assert.That(stage.ProgressPercent, Is.EqualTo(50));
        Assert.That(stage.Status, Is.EqualTo(StageStatus.InProgress));
    }

    /// <summary>
    /// Тест создания этапа для несуществующего проекта
    /// </summary>
    [Test]
    public async Task CreateStageAsync_NonExistentProject()
    {
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "Development",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 0,
            Status = "Pending",
            SpecialistUserId = _testUser.UserId
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _stagesService.CreateStageAsync(request, 99999, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не найден"));
    }

    /// <summary>
    /// Тест создания этапа с несуществующим специалистом
    /// </summary>
    [Test]
    public async Task CreateStageAsync_NonExistentSpecialist()
    {
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "Development",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 0,
            Status = "Pending",
            SpecialistUserId = 99999 // Несуществующий пользователь
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _stagesService.CreateStageAsync(request, _testProject.ProjectId, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не найден"));
    }

    /// <summary>
    /// Тест создания этапа с невалидным типом этапа
    /// </summary>
    [Test]
    public async Task CreateStageAsync_InvalidStageType()
    {
        var request = new CreateStageRequest
        {
            Name = "Test Stage",
            StageType = "InvalidType",
            Deadline = DateTime.UtcNow.AddDays(10),
            ProgressPercent = 0,
            Status = "Pending",
            SpecialistUserId = _testUser.UserId
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _stagesService.CreateStageAsync(request, _testProject.ProjectId, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("не является корректным значением"));
    }
}

