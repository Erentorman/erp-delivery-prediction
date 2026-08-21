using App.Application.Prediction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Integration.AiPrediction;

public static class AiPredictionServiceCollectionExtensions
{
    public static IServiceCollection AddAiPredictionClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetRequiredSection(AiPredictionOptions.SectionName)
            .Get<AiPredictionOptions>()
            ?? throw new InvalidOperationException("AiPrediction configuration is missing.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("AiPrediction:BaseUrl must be an absolute HTTP or HTTPS URI.");
        }

        if (options.TimeoutMs <= 0)
        {
            throw new InvalidOperationException("AiPrediction:TimeoutMs must be positive.");
        }

        services.AddSingleton(options);
        services.AddTransient<AiPredictionProvider>();
        services.AddHttpClient<IAiPredictionClient, FastApiPredictionClient>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        return services;
    }
}
