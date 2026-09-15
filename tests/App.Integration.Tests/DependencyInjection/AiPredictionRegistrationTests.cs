using App.Application.IntegrationLogging;
using App.Application.Prediction;
using App.Integration.AiPrediction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace App.Integration.Tests.DependencyInjection;

public sealed class AiPredictionRegistrationTests
{
    [Fact]
    public void AddAiPredictionClient_ResolvesClientAndProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiPrediction:BaseUrl"] = "https://ai.test",
                ["AiPrediction:TimeoutMs"] = "47"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IIntegrationLogWriter>());
        services.AddSingleton<IAiFeatureBuilder, AiFeatureBuilder>();
        services.AddAiPredictionClient(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.IsType<FastApiPredictionClient>(provider.GetRequiredService<IAiPredictionClient>());
        Assert.IsType<AiPredictionProvider>(provider.GetRequiredService<AiPredictionProvider>());
        Assert.Equal(47, provider.GetRequiredService<AiPredictionOptions>().TimeoutMs);
    }

    [Theory]
    [InlineData("not-a-url", "3000")]
    [InlineData("https://ai.test", "0")]
    public void AddAiPredictionClient_WithInvalidConfiguration_FailsAtRegistration(
        string baseUrl,
        string timeoutMs)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiPrediction:BaseUrl"] = baseUrl,
                ["AiPrediction:TimeoutMs"] = timeoutMs
            })
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddAiPredictionClient(configuration));
    }
}
