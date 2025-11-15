using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Domain.Entities;
using Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
{
    // Используем Npgsql для подключения к PostgreSQL
    options.UseNpgsql(connectionString, 
        // Дополнительные настройки для Npgsql
        b => b.MigrationsAssembly("ProjectManagement.Infrastructure"));
});

// Настройка JWT Авторизации ---
var jwtSecretKey = builder.Configuration["Jwt:Key"] ?? 
                   throw new InvalidOperationException("JWT Key is not configured.");
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });
// Настройка контроллеров
builder.Services.AddControllers();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Добавляем поддержку JWT в Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите токен (только сам токен без 'Bearer ')"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Если среда разработки - Development (например, при отладке)
if (app.Environment.IsDevelopment())
{
    // Используем Swagger и Seed Data
    app.UseSwagger();
    app.UseSwaggerUI();

    // 💡 Вызов функции создания базы и добавления базовых данных
    await SeedDataAsync(app); 
}

// Перенаправление HTTP на HTTPS (рекомендуется)
app.UseHttpsRedirection();

// Важно: Аутентификация должна идти перед Авторизацией
app.UseAuthentication(); 
app.UseAuthorization();

// Маппинг контроллеров
app.MapControllers();

app.Run();

async Task SeedDataAsync(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProjectManagementDbContext>();
        
        // Применяем миграции (создаем БД, если она еще не создана)
        await context.Database.MigrateAsync();

        // 💡 Создание базовых данных (если нет пользователей)
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = "hashed_admin_password",
                Role = UserRole.Administrator,
                FullName = "Администратор системы"
            };
            var specUser = new User
            {
                Username = "specialist",
                PasswordHash = "hashed_spec_password",
                Role = UserRole.Specialist,
                FullName = "Специалист по проектам"
            };
            
            context.Users.AddRange(adminUser, specUser);
            await context.SaveChangesAsync();

            var testProject = new Project
            {
                Name = "Тестовый проект 1",
                Description = "Симулированный исторический проект.",
                Budget = 50000.00m,
                StartDate = DateTime.Today.AddDays(-60),
                EndDate = DateTime.Today.AddDays(30),
                Status = ProjectStatus.Active,
                CreatedByUserId = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            };
            context.Projects.Add(testProject);
            await context.SaveChangesAsync();

            // 💡 Создание тестового этапа
            var testStage = new Stage
            {
                ProjectId = testProject.ProjectId,
                Name = "Этап изысканий",
                StageType = StageType.Exploration,
                Deadline = DateTime.Today.AddDays(15),
                ProgressPercent = 50,
                Status = StageStatus.InProgress,
                SpecialistUserId = specUser.UserId
            };
            context.Stages.Add(testStage);
            await context.SaveChangesAsync();
            
            Console.WriteLine("Базовые тестовые данные успешно добавлены.");
        }
    }
}