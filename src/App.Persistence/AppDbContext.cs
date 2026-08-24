using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();
    public DbSet<PredictionProviderResultEntity> PredictionProviderResults => Set<PredictionProviderResultEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
