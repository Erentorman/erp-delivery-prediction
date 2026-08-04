using App.Application.Abstractions.Erp;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application.Erp;

public static class ApplicationErpServiceCollectionExtensions
{
    public static IServiceCollection AddErpBatchReader(this IServiceCollection services)
    {
        services.AddScoped<IErpBatchReader, ErpBatchReader>();
        return services;
    }
}
