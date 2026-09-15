using App.Application.Contracts.Configuration;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionOrchestrator
{
    private readonly IPredictionProvider _ruleBased;
    private readonly IPredictionProvider _ai;
    private readonly IFinalPredictionCombiner _combiner;
    private readonly IPredictionRepository _repository;
    private readonly MvpAssumptionsOptions _options;

    public PredictionOrchestrator(IEnumerable<IPredictionProvider> providers, IFinalPredictionCombiner combiner,
        IPredictionRepository repository, MvpAssumptionsOptions options)
    {
        var all = providers.ToList();
        _ruleBased = all.Single(p => p.ProviderType == PredictionProviderType.RuleBased);
        _ai = all.Single(p => p.ProviderType == PredictionProviderType.Ai);
        _combiner = combiner;
        _repository = repository;
        _options = options;
    }

    public async Task<PredictionAggregateResult> ExecuteAsync(PredictionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var aiTask = _ai.PredictAsync(context, cancellationToken);
        var rule = await _ruleBased.PredictAsync(context, cancellationToken);
        var ai = await aiTask;
        var final = AddCalendarDates(_combiner.Combine(rule, ai), rule);
        var aggregate = new PredictionAggregateResult(context.OrderInput.OrderReference, rule, ai, final);
        await _repository.SaveAsync(aggregate, cancellationToken);
        return aggregate;
    }

    private FinalPredictionResult AddCalendarDates(FinalPredictionResult final, PredictionProviderResult rule)
    {
        if (final.WorkingLeadTimeMinutes is not long minutes || rule.RuleBasedPrediction is null ||
            final.Status is FinalPredictionStatus.AiOnlyCandidate or FinalPredictionStatus.InsufficientData)
            return final;

        if (final.Status == FinalPredictionStatus.RuleBasedFallback)
            return final with { EstimatedStart = rule.RuleBasedPrediction.EstimatedStart,
                EstimatedEnd = rule.RuleBasedPrediction.EstimatedEnd,
                EstimatedDelivery = rule.RuleBasedPrediction.EstimatedDelivery };

        var calendar = new WorkingCalendar(_options.WorkingCalendar.MinutesPerDay);
        var start = rule.RuleBasedPrediction.EstimatedStart;
        var end = calendar.AddWorkingMinutes(start, minutes);
        var shipping = rule.RuleBasedPrediction.EstimatedDelivery - rule.RuleBasedPrediction.EstimatedEnd;
        return final with { EstimatedStart = start, EstimatedEnd = end, EstimatedDelivery = end.Add(shipping) };
    }
}
