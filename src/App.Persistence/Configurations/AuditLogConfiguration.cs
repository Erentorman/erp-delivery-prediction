using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(log => log.IntegrationLogId)
            .HasColumnName("integration_log_id")
            .IsRequired();

        builder.Property(log => log.IntegrationType)
            .HasColumnName("integration_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Operation)
            .HasColumnName("operation")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(log => log.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne(log => log.IntegrationLog)
            .WithOne(log => log.AuditLog)
            .HasForeignKey<AuditLog>(log => log.IntegrationLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(log => log.CreatedAt);
    }
}
