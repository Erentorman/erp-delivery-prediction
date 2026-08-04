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

        // 2. Check config version
        if (options.ConfigVersion != "2.0")
        {
            throw new InvalidOperationException($"Unexpected configVersion. Expected '2.0', got '{options.ConfigVersion}'.");
        }

        // 3. Check netMinutesPerDay consistency
        if (options.WorkingCalendar != null && 
            !string.IsNullOrWhiteSpace(options.WorkingCalendar.StartTime) && 
            !string.IsNullOrWhiteSpace(options.WorkingCalendar.EndTime))
        {
            if (TimeSpan.TryParse(options.WorkingCalendar.StartTime, out var startTime) &&
                TimeSpan.TryParse(options.WorkingCalendar.EndTime, out var endTime))
            {
                var calculatedNetMinutes = (int)(endTime - startTime).TotalMinutes - options.WorkingCalendar.BreakMinutes;
                if (calculatedNetMinutes != options.WorkingCalendar.NetMinutesPerDay)
                {
                    throw new InvalidOperationException($"netMinutesPerDay is inconsistent. Expected {calculatedNetMinutes}, got {options.WorkingCalendar.NetMinutesPerDay}.");
                }
            }
        }

        // 4. Check missing shipping unknownRouteFallbackMinutes
        if (options.Shipping?.UnknownRouteFallbackMinutes == null)
        {
            logger.LogWarning("shipping.unknownRouteFallbackMinutes is null. Fallback operations will not be available for unknown routes.");
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
