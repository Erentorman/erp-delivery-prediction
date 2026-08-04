using System.Text.Json;
using App.Application.Contracts.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Api.Configuration;

public static class CategoryAFieldGuard
{
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "machineCount",
        "defaultMachineCount",
        "defaultMachineCountPerWorkCenter",
        "operationDuration",
        "operationDurationFallback",
        "durationBasis",
        "durationBasisFallback",
        "alternativeWorkCenter",
        "alternativeWorkCenterPriority"
    };

    public static void Validate(string jsonContent, MvpAssumptionsOptions options, ILogger logger)
    {
        // 1. Check for Category A forbidden fields
        using var doc = JsonDocument.Parse(jsonContent);
        CheckElement(doc.RootElement);

        if (options.Shipping.FallbackDurationMinutes == null)
        {
            logger.LogWarning("shipping.fallbackDurationMinutes is null. Fallback operations will not be available for unknown routes.");
        }
    }

    private static void CheckElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ForbiddenKeys.Contains(property.Name))
                {
                    throw new InvalidOperationException($"Forbidden Category A field found in config: {property.Name}");
                }
                CheckElement(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CheckElement(item);
            }
        }
    }
}
