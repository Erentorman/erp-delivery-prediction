using System.Text.Json;
using App.Application.Prediction;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Persistence.Prediction;

public sealed class PredictionRepository : IPredictionRepository
{
    private const string RuleBasedProviderType = "RuleBased";
    private const string SuccessProviderStatus = "Success";
    private const string CalculatedStatus = "Calculated";
    private const string CalculatedWithAssumptionsStatus = "CalculatedWithAssumptions";
    private const string FullDataSufficiency = "Full";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _dbContext;

    public PredictionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SaveAsync(PredictionPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = request.Result;
        var calculatedAt = result.EstimatedStart.UtcDateTime;
        var hasFallbacks = result.AppliedFallbackReasons.Count > 0;

        var criticalPathSummary = JsonSerializer.Serialize(
            new
            {
                criticalPath = result.CriticalPathOperations,
                timeline = result.Timeline,
                shortages = result.Shortages
            },
            JsonOptions);

        var workingLeadTimeMinutes = (long)Math.Round((result.EstimatedEnd - result.EstimatedStart).TotalMinutes);

        var predictionResult = new PredictionResult
        {
            ErpOrderRef = request.ErpOrderRef,
            IsSimulation = request.IsSimulation,
            SimulationInputSummary = request.SimulationInput is null
                ? null
                : JsonSerializer.Serialize(request.SimulationInput, JsonOptions),
            Status = hasFallbacks ? CalculatedWithAssumptionsStatus : CalculatedStatus,
            DataSufficiencyLevel = FullDataSufficiency,
            FinalWorkingLeadTimeMinutes = workingLeadTimeMinutes,
            ProductionStart = result.EstimatedStart.UtcDateTime,
            ProductionEnd = result.EstimatedEnd.UtcDateTime,
            ShipDate = result.EstimatedEnd.UtcDateTime,
            DeliveryDate = result.EstimatedDelivery.UtcDateTime,
            RequestedDeliveryDate = request.RequestedDeliveryDate?.UtcDateTime,
            CriticalPathSummary = criticalPathSummary,
            CalculatedAt = calculatedAt
        };

        var providerResult = new PredictionProviderResult
        {
            PredictionResult = predictionResult,
            ProviderType = RuleBasedProviderType,
            ProviderStatus = SuccessProviderStatus,
            WorkingLeadTimeMinutes = workingLeadTimeMinutes,
            EstimatedDeliveryDate = result.EstimatedDelivery.UtcDateTime,
            Warnings = JsonSerializer.Serialize(result.AppliedFallbackReasons, JsonOptions),
            CreatedAt = calculatedAt
        };

        _dbContext.PredictionResults.Add(predictionResult);
        _dbContext.PredictionProviderResults.Add(providerResult);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionHistoryListItem>> GetHistoryAsync(
        string? orderReference,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PredictionResults.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderReference))
        {
            query = query.Where(p => p.ErpOrderRef == orderReference);
        }

        var effectivePage = page < 1 ? 1 : page;
        var effectivePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var items = await query
            .OrderByDescending(p => p.CalculatedAt)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(p => new PredictionHistoryListItem(
                p.Id,
                p.ErpOrderRef,
                p.IsSimulation,
                p.Status,
                p.DataSufficiencyLevel,
                p.FinalWorkingLeadTimeMinutes,
                p.DeliveryDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(p.DeliveryDate.Value, DateTimeKind.Utc)) : null,
                new DateTimeOffset(DateTime.SpecifyKind(p.CalculatedAt, DateTimeKind.Utc))))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<PredictionHistoryDetail?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PredictionResults
            .AsNoTracking()
            .Include(p => p.ProviderResults)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new PredictionHistoryDetail(
            entity.Id,
            entity.ErpOrderRef,
            entity.IsSimulation,
            entity.SimulationInputSummary,
            entity.Status,
            entity.DataSufficiencyLevel,
            entity.FinalWorkingLeadTimeMinutes,
            ToUtcOffset(entity.ProductionStart),
            ToUtcOffset(entity.ProductionEnd),
            ToUtcOffset(entity.ShipDate),
            ToUtcOffset(entity.DeliveryDate),
            ToUtcOffset(entity.RequestedDeliveryDate),
            entity.CriticalPathSummary,
            new DateTimeOffset(DateTime.SpecifyKind(entity.CalculatedAt, DateTimeKind.Utc)),
            ToUtcOffset(entity.ActualDeliveryDate),
            entity.ActualTotalWorkingLeadTimeMinutes,
            entity.DeliveredLate,
            entity.ProviderResults
                .Select(pr => new PredictionHistoryProviderResult(
                    pr.ProviderType,
                    pr.ProviderStatus,
                    pr.WorkingLeadTimeMinutes,
                    ToUtcOffset(pr.EstimatedDeliveryDate),
                    pr.ModelVersion,
                    pr.FeatureSchemaVersion,
                    pr.TrainingDatasetVersion,
                    pr.Warnings))
                .ToList());
    }

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
        => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;
}
