using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Persistence.Configurations;

public class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> builder)
    {
        builder.ToTable("PredictionResults");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(p => p.ErpOrderRef)
            .HasColumnName("erp_order_ref")
            .HasMaxLength(100);

        builder.Property(p => p.IsSimulation)
            .HasColumnName("is_simulation")
            .IsRequired();

        builder.Property(p => p.SimulationInputSummary)
            .HasColumnName("simulation_input_summary")
            .HasColumnType("jsonb");

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.DataSufficiencyLevel)
            .HasColumnName("data_sufficiency_level")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.FinalWorkingLeadTimeMinutes)
            .HasColumnName("final_working_lead_time_minutes");

        builder.Property(p => p.ProductionStart)
            .HasColumnName("production_start")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ProductionEnd)
            .HasColumnName("production_end")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ShipDate)
            .HasColumnName("ship_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.DeliveryDate)
            .HasColumnName("delivery_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.RequestedDeliveryDate)
            .HasColumnName("requested_delivery_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.CriticalPathSummary)
            .HasColumnName("critical_path_summary")
            .HasColumnType("jsonb");

        builder.Property(p => p.CalculatedAt)
            .HasColumnName("calculated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.ActualProductionStart)
            .HasColumnName("actual_production_start")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ActualProductionEnd)
            .HasColumnName("actual_production_end")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ActualShippingDate)
            .HasColumnName("actual_shipping_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ActualDeliveryDate)
            .HasColumnName("actual_delivery_date")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ActualTotalWorkingLeadTimeMinutes)
            .HasColumnName("actual_total_working_lead_time_minutes");

        builder.Property(p => p.DeliveredLate)
            .HasColumnName("delivered_late");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by");

        builder.HasIndex(p => p.ErpOrderRef);
        builder.HasIndex(p => p.CalculatedAt);
        builder.HasIndex(p => p.IsSimulation);

        builder.HasMany(p => p.ProviderResults)
            .WithOne(pr => pr.PredictionResult)
            .HasForeignKey(pr => pr.PredictionResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
