using App.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.Clock;

public static class InfrastructureClockServiceCollectionExtensions
{
    public static IServiceCollection AddSystemClock(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
