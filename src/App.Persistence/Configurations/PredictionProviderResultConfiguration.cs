using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Persistence.Configurations;

public class PredictionProviderResultConfiguration : IEntityTypeConfiguration<PredictionProviderResult>
{
    public void Configure(EntityTypeBuilder<PredictionProviderResult> builder)
    {
        builder.ToTable("PredictionProviderResults");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(p => p.PredictionResultId)
            .HasColumnName("prediction_result_id")
            .IsRequired();

        builder.Property(p => p.ProviderType)
            .HasColumnName("provider_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ProviderStatus)
            .HasColumnName("provider_status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.WorkingLeadTimeMinutes)
            .HasColumnName("working_lead_time_minutes");

        builder.Property(p => p.EstimatedDeliveryDate)
            .HasColumnName("estimated_delivery_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ModelVersion)
            .HasColumnName("model_version")
            .HasMaxLength(50);

        builder.Property(p => p.FeatureSchemaVersion)
            .HasColumnName("feature_schema_version")
            .HasMaxLength(50);

        builder.Property(p => p.TrainingDatasetVersion)
            .HasColumnName("training_dataset_version")
            .HasMaxLength(50);

        builder.Property(p => p.FeaturePayload)
            .HasColumnName("feature_payload")
            .HasColumnType("jsonb");

        builder.Property(p => p.Warnings)
            .HasColumnName("warnings")
            .HasColumnType("jsonb");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(p => new { p.PredictionResultId, p.ProviderType }).IsUnique();
        builder.HasIndex(p => p.ProviderType);
    }
}
