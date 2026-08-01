using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Persistence.Configurations;

public class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        builder.ToTable("IntegrationLogs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(log => log.IntegrationType)
            .HasColumnName("integration_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Operation)
            .HasColumnName("operation")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(log => log.ExternalResource)
            .HasColumnName("external_resource")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(log => log.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(log => log.StatusCode)
            .HasColumnName("status_code");

        builder.Property(log => log.DurationMs)
            .HasColumnName("duration_ms")
            .IsRequired();

        builder.Property(log => log.Message)
            .HasColumnName("message")
            .HasMaxLength(1000);

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(log => log.IntegrationType);
        builder.HasIndex(log => log.CreatedAt);
        builder.HasIndex(log => new { log.IntegrationType, log.Operation });
    }
}
