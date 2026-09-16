using App.Application.Contracts.Configuration;

namespace App.Api.Configuration;

public static class MvpAssumptionsConfigurationExtensions
{
    public static IConfigurationBuilder AddMvpAssumptions(this IConfigurationBuilder configuration)
    {
        return configuration.AddJsonFile(
            "mvp-assumptions.json",
            optional: false,
            reloadOnChange: true);
    }

    public static IServiceCollection AddMvpAssumptionsOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MvpAssumptionsOptions>()
            .Bind(configuration.GetSection(MvpAssumptionsOptions.SectionName))
            .Validate(
                options => options.WorkingCalendar.StartTime < options.WorkingCalendar.EndTime,
                "Working calendar start time must be before end time.")
            .Validate(
                options => options.WorkingCalendar.BreakStartTime <= options.WorkingCalendar.BreakEndTime,
                "Working calendar break start time must not be after break end time.")
            .Validate(
                options => options.WorkingCalendar.BreakStartTime >= options.WorkingCalendar.StartTime &&
                           options.WorkingCalendar.BreakEndTime <= options.WorkingCalendar.EndTime,
                "Working calendar break window must fall within the shift window.")
            .Validate(
                options => (options.WorkingCalendar.BreakEndTime - options.WorkingCalendar.BreakStartTime).TotalMinutes
                           == options.WorkingCalendar.BreakMinutes,
                "Working calendar break minutes must match (breakEndTime - breakStartTime).")
            .Validate(
                options => options.WorkingCalendar.NetMinutesPerDay > 0,
                "Working calendar net minutes per day must be greater than zero.")
            .Validate(
                options => (options.WorkingCalendar.EndTime - options.WorkingCalendar.StartTime).TotalMinutes
                           - options.WorkingCalendar.BreakMinutes == options.WorkingCalendar.NetMinutesPerDay,
                "Working calendar net minutes per day must equal (endTime - startTime) minus breakMinutes.")
            .Validate(
                options => options.WorkingCalendar.WorkingDays.Count > 0 &&
                           options.WorkingCalendar.WorkingDays.Distinct().Count() == options.WorkingCalendar.WorkingDays.Count,
                "Working calendar working days must be non-empty and contain no duplicates.")
            .Validate(
                options => options.Procurement.FallbackDurationMinutes > 0,
                "Procurement fallback duration must be greater than zero.")
            .Validate(
                options => options.Shipping.FallbackDurationMinutes is null or > 0,
                "Shipping fallback duration must be null or greater than zero.")
            .Validate(
                options => options.HybridPrediction.RuleBasedWeight >= 0 &&
                           options.HybridPrediction.AiWeight >= 0 &&
                           options.HybridPrediction.AiVarianceThresholdPercent >= 0 &&
                           options.HybridPrediction.AiVarianceThresholdWorkingMinutes >= 0 &&
                           options.HybridPrediction.AiTechnicalUpperBoundWorkingMinutes is null or > 0,
                "Hybrid prediction weights and thresholds must be non-negative and an optional upper bound must be positive.")
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MvpAssumptionsOptions>>().Value);

        return services;
    }
}
