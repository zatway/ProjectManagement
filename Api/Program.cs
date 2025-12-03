using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Services;
using Infrastructure.SignalR.Hubs;
using Application.Interfaces.SignalR;
using Infrastructure.Services.SignalR;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// --- 1. Настройка Подключения к БД ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
}

builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
{
    options.UseNpgsql(connectionString,
        b => b.MigrationsAssembly("ProjectManagement.Infrastructure"));
});

builder.Services.AddDbContextFactory<ProjectManagementDbContext>(
    options =>  // Action<DbContextOptionsBuilder>
    {
        options.UseNpgsql(connectionString,
            b => b.MigrationsAssembly("ProjectManagement.Infrastructure"));
    },
    ServiceLifetime.Scoped);

// В builder.Services (после AddDbContext, перед AddAuthentication)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:5174") // Добавьте свои
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); // Для SignalR WebSocket
    });
});

// --- 2. Настройка JWT Авторизации ---
var jwtSecretKey = builder.Configuration["Jwt:Key"] ??
                   throw new InvalidOperationException("JWT Key is not configured.");
var issuer = builder.Configuration["Jwt:Issuer"] ??
             throw new InvalidOperationException("JWT Issuer is not configured.");
var audience = builder.Configuration["Jwt:Audience"] ??
               throw new InvalidOperationException("JWT Audience is not configured.");

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

        // 💡 Настройка для SignalR: Извлекаем токен из Query String или заголовка Authorization
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerEvents");

                var path = context.HttpContext.Request.Path;
                
                if (path.StartsWithSegments("/hubs/notifications"))
                {
                    var accessToken = context.Request.Query["access_token"];
                    
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            accessToken = authHeader.Substring("Bearer ".Length).Trim();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerEvents");

                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs/notifications"))
                {
                    context.NoResult();
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerEvents");

                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs/notifications"))
                {
                    logger.LogDebug(
                        "JWT challenge triggered for SignalR. Error: {Error}, Description: {Description}",
                        context.Error,
                        context.ErrorDescription);

                    context.HandleResponse();
                }
                return Task.CompletedTask;
            }
        };
    });

// Настройка SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Включаем детальные ошибки для отладки
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Keep-alive для соединения
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Таймаут клиента
});

builder.Services.AddControllers();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IStageService, StagesService>();
builder.Services.AddScoped<IUsersService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
            new string[] { }
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // await SeedDataAsync(app);
}

app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();

app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

    endpoints.MapHub<NotificationHub>("/hubs/notifications");
});

app.Run();

async Task SeedDataAsync(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProjectManagementDbContext>();

        await context.Database.MigrateAsync();

        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash =
                    "$2a$12$Nq5bW2V8d4Dk4vK6v8j0lO/M.yF6zS7E0yH1wP4nZqX.yH1zH0e8c", // Хэш для пароля "admin123" (используйте BCrypt для реальных)
                Role = UserRole.Administrator,
                FullName = "Администратор системы"
            };
            var specUser = new User
            {
                Username = "specialist",
                PasswordHash =
                    "$2a$12$Nq5bW2V8d4Dk4vK6v8j0lO/M.yF6zS7E0yH1wP4nZqX.yH1zH0e8c", // Хэш для "password"
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

            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("SeedData");
            logger.LogInformation("Базовые тестовые данные успешно добавлены.");
        }
    }
}