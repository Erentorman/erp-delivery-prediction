using App.Application.Prediction.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application.Prediction;

public static class ApplicationPredictionServiceCollectionExtensions
{
    public static IServiceCollection AddPredictionServices(
        this IServiceCollection services)
    {
        services.AddTransient<PredictionContextBuilder>();
        services.AddTransient<IAiFeatureBuilder, AiFeatureBuilder>();

        services.AddTransient<IPredictionContextBuilder>(
            serviceProvider =>
                serviceProvider.GetRequiredService<PredictionContextBuilder>());

        services.AddTransient<WhatIfPredictionContextBuilder>();

        services.AddTransient<ProcurementResolver>();
        services.AddTransient<ShippingResolver>();
        services.AddTransient<CapacityResolver>();

        services.AddTransient<
            App.Domain.Prediction.ICriticalPathCalculator,
            App.Domain.Prediction.CriticalPathCalculator>();

        services.AddTransient<RuleBasedPredictionEngine>();
        services.AddTransient<PredictionResultMapper>();
        services.AddTransient<RuleBasedPredictionProvider>();
        services.AddTransient<IPredictionProvider>(sp => sp.GetRequiredService<RuleBasedPredictionProvider>());
        services.AddTransient<IPredictionProvider>(sp => sp.GetRequiredService<AiPredictionProvider>());
        services.AddTransient<IFinalPredictionCombiner, FinalPredictionCombiner>();
        services.AddTransient<PredictionOrchestrator>();

        services.AddTransient<
            IPredictionCalculationService,
            PredictionCalculationService>();

        services.AddTransient<
            IWhatIfPredictionCalculationService,
            WhatIfPredictionCalculationService>();

        return services;
    }
}
