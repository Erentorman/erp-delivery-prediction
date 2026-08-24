using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Persistence.Configurations;

public sealed class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> b)
    {
        b.ToTable("PredictionResults"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.ErpOrderReference).HasColumnName("erp_order_ref").HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        b.Property(x => x.FinalStatus).HasColumnName("final_status").HasMaxLength(40).IsRequired();
        b.Property(x => x.FallbackReason).HasColumnName("fallback_reason").HasMaxLength(60).IsRequired();
        b.Property(x => x.CombinationStrategy).HasColumnName("combination_strategy").HasMaxLength(60);
        b.Property(x => x.RuleBasedWeight).HasColumnName("rule_based_weight").HasPrecision(4, 2);
        b.Property(x => x.AiWeight).HasColumnName("ai_weight").HasPrecision(4, 2);
        b.Property(x => x.FinalWorkingLeadTimeMinutes).HasColumnName("final_working_lead_time_minutes");
        b.Property(x => x.AbsoluteDifferenceMinutes).HasColumnName("absolute_difference_minutes");
        b.Property(x => x.RelativeDifferencePercent).HasColumnName("relative_difference_percent").HasPrecision(6, 2);
        b.Property(x => x.ProductionStart).HasColumnName("production_start").HasColumnType("timestamptz");
        b.Property(x => x.ProductionEnd).HasColumnName("production_end").HasColumnType("timestamptz");
        b.Property(x => x.DeliveryDate).HasColumnName("delivery_date").HasColumnType("timestamptz");
        b.Property(x => x.CalculatedAt).HasColumnName("calculated_at").HasColumnType("timestamptz").IsRequired();
        b.HasIndex(x => x.ErpOrderReference); b.HasIndex(x => x.FinalStatus); b.HasIndex(x => x.CalculatedAt);
    }
}

public sealed class PredictionProviderResultConfiguration : IEntityTypeConfiguration<PredictionProviderResultEntity>
{
    public void Configure(EntityTypeBuilder<PredictionProviderResultEntity> b)
    {
        b.ToTable("PredictionProviderResults"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PredictionResultId).HasColumnName("prediction_result_id");
        b.Property(x => x.ProviderType).HasColumnName("provider_type").HasMaxLength(20).IsRequired();
        b.Property(x => x.ProviderStatus).HasColumnName("provider_status").HasMaxLength(30).IsRequired();
        b.Property(x => x.WorkingLeadTimeMinutes).HasColumnName("working_lead_time_minutes");
        b.Property(x => x.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date").HasColumnType("timestamptz");
        b.Property(x => x.ModelVersion).HasColumnName("model_version").HasMaxLength(50);
        b.Property(x => x.FeatureSchemaVersion).HasColumnName("feature_schema_version").HasMaxLength(50);
        b.Property(x => x.TrainingDatasetVersion).HasColumnName("training_dataset_version").HasMaxLength(50);
        b.Property(x => x.FeaturePayload).HasColumnName("feature_payload").HasColumnType("jsonb");
        b.Property(x => x.Warnings).HasColumnName("warnings").HasColumnType("jsonb");
        b.Property(x => x.DurationMs).HasColumnName("duration_ms");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        b.HasOne(x => x.PredictionResult).WithMany(x => x.ProviderResults).HasForeignKey(x => x.PredictionResultId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.PredictionResultId, x.ProviderType }).IsUnique();
        b.HasIndex(x => x.ProviderType);
    }
}
