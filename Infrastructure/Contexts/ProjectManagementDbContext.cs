using Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Контекст базы данных для управления проектами
/// </summary>
public class ProjectManagementDbContext : DbContext
{
    public ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options)
        : base(options)
    {
    }

    // Свойства DbSet представляют коллекции сущностей в базе данных (таблицы)
    
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    
    public DbSet<Stage> Stages { get; set; }
    
    public DbSet<Report> Reports { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }

    /// <summary>
    /// Настройка связей и ограничений между сущностями.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Ограничения на поле Username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // 2. Связи для Project
        modelBuilder.Entity<Project>()
            .HasOne(p => p.CreatedBy) 
            .WithMany(u => u.CreatedProjects)
            .HasForeignKey(p => p.CreatedByUserId) // Использование внешнего ключа
            .OnDelete(DeleteBehavior.Restrict); // Не удаляем пользователя, если у него есть проекты

        // 3. Связи для Stage
        modelBuilder.Entity<Stage>()
            .HasOne(s => s.Project)
            .WithMany(p => p.Stages)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);// При удалении проекта удаляются и все его этапы

        modelBuilder.Entity<Stage>()
            .HasOne(s => s.Specialist)
            .WithMany(u => u.AssignedStages)
            .HasForeignKey(s => s.SpecialistUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Связи для Report
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Project)
            .WithMany(p => p.Reports)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Stage)
            .WithMany(s => s.Reports)
            .HasForeignKey(r => r.StageId)
            .IsRequired(false) // StageId может быть null
            .OnDelete(DeleteBehavior.Restrict); 
        
        // 5. Связи для Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Project)
            .WithMany() // У проекта нет обратной коллекции уведомлений (опционально)
            .HasForeignKey(n => n.ProjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull); // При удалении проекта, ProjectId в уведомлениях становится NULL

        base.OnModelCreating(modelBuilder);
    }
}