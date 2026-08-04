using Microsoft.Extensions.DependencyInjection;

namespace App.Application.Prediction;

public static class ApplicationPredictionServiceCollectionExtensions
{
    public static IServiceCollection AddPredictionServices(this IServiceCollection services)
    {
        services.AddTransient<PredictionContextBuilder>();
        return services;
    }
}
