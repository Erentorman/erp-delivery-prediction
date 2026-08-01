using App.Application.Abstractions.Erp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Integration.MockErp;

public static class MockErpServiceCollectionExtensions
{
    public static IServiceCollection AddMockErpDataProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetRequiredSection(MockErpOptions.SectionName).Get<MockErpOptions>()
            ?? throw new InvalidOperationException("MockErp configuration is missing.");
        if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("MockErp:BaseAddress must be an absolute HTTP or HTTPS URI.");
        }

        services.AddHttpClient<IErpDataProvider, MockErpDataProvider>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        return services;
    }
}
