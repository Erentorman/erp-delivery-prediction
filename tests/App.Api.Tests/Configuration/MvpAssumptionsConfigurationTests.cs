using System.Text.Json;
using App.Api.Configuration;
using App.Application.Contracts.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace App.Api.Tests.Configuration;

public class MvpAssumptionsConfigurationTests
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "mvp-assumptions.json");

    [Fact]
    public void ConfigurationFile_HasExactExpectedShapeAndValues()
    {
        Assert.True(File.Exists(ConfigPath));

        using var document = JsonDocument.Parse(File.ReadAllText(ConfigPath));
        var rootProperties = document.RootElement.EnumerateObject().ToArray();
        Assert.Single(rootProperties);
        Assert.Equal(MvpAssumptionsOptions.SectionName, rootProperties[0].Name);

        var groups = rootProperties[0].Value.EnumerateObject().ToArray();
        Assert.Equal(new[] { "workingCalendar", "procurement", "shipping", "hybridPrediction" }, groups.Select(group => group.Name));

        var workingCalendar = groups[0].Value;
        Assert.Equal(
            new[] { "startTime", "endTime", "breakStartTime", "breakEndTime", "breakMinutes", "netMinutesPerDay", "workingDays" },
            workingCalendar.EnumerateObject().Select(property => property.Name));
        Assert.Equal("08:00", workingCalendar.GetProperty("startTime").GetString());
        Assert.Equal("17:00", workingCalendar.GetProperty("endTime").GetString());
        Assert.Equal("12:00", workingCalendar.GetProperty("breakStartTime").GetString());
        Assert.Equal("13:00", workingCalendar.GetProperty("breakEndTime").GetString());
        Assert.Equal(60, workingCalendar.GetProperty("breakMinutes").GetInt64());
        Assert.Equal(480, workingCalendar.GetProperty("netMinutesPerDay").GetInt64());
        Assert.Equal(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            workingCalendar.GetProperty("workingDays").EnumerateArray().Select(day => day.GetString()));

        AssertSingleProperty(groups[1].Value, "fallbackDurationMinutes", JsonValueKind.Number);
        AssertSingleProperty(groups[2].Value, "fallbackDurationMinutes", JsonValueKind.Null);
        Assert.Equal(960, groups[1].Value.GetProperty("fallbackDurationMinutes").GetInt64());
        var hybrid = groups[3].Value;
        Assert.Equal(.60m, hybrid.GetProperty("ruleBasedWeight").GetDecimal());
        Assert.Equal(.40m, hybrid.GetProperty("aiWeight").GetDecimal());
        Assert.Equal(50m, hybrid.GetProperty("aiVarianceThresholdPercent").GetDecimal());
        Assert.Equal(960, hybrid.GetProperty("aiVarianceThresholdWorkingMinutes").GetInt64());
        Assert.Equal(JsonValueKind.Null, hybrid.GetProperty("aiTechnicalUpperBoundWorkingMinutes").ValueKind);
    }

    [Fact]
    public void ConfigurationFile_ContainsNoMachineCountOrErpReferenceFields()
    {
        var json = File.ReadAllText(ConfigPath);
        var forbiddenFields = new[]
        {
            "machineCount", "defaultMachineCount", "workCenterRef", "workCenterReference",
            "productReference", "orderReference", "supplierReference", "originReference",
            "destinationReference", "shippingProfileReference"
        };

        Assert.All(forbiddenFields, field => Assert.DoesNotContain(field, json, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OptionsTypes_DoNotExposeMachineCount()
    {
        var optionTypes = new[]
        {
            typeof(MvpAssumptionsOptions), typeof(WorkingCalendarAssumptionsOptions),
            typeof(ProcurementAssumptionsOptions), typeof(ShippingAssumptionsOptions)
        };

        Assert.All(optionTypes, type =>
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name.Contains("MachineCount", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Configuration_BindsAndResolvesThroughIOptions()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddMvpAssumptions()
            .Build();
        var services = new ServiceCollection();
        services.AddMvpAssumptionsOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;

        Assert.Equal(new TimeOnly(8, 0), options.WorkingCalendar.StartTime);
        Assert.Equal(new TimeOnly(17, 0), options.WorkingCalendar.EndTime);
        Assert.Equal(new TimeOnly(12, 0), options.WorkingCalendar.BreakStartTime);
        Assert.Equal(new TimeOnly(13, 0), options.WorkingCalendar.BreakEndTime);
        Assert.Equal(60, options.WorkingCalendar.BreakMinutes);
        Assert.Equal(480, options.WorkingCalendar.NetMinutesPerDay);
        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            options.WorkingCalendar.WorkingDays);
        Assert.Equal(960, options.Procurement.FallbackDurationMinutes);
        Assert.Null(options.Shipping.FallbackDurationMinutes);
        Assert.Equal(.60m, options.HybridPrediction.RuleBasedWeight);
        Assert.Equal(.40m, options.HybridPrediction.AiWeight);
        Assert.Equal(50m, options.HybridPrediction.AiVarianceThresholdPercent);
        Assert.Equal(960, options.HybridPrediction.AiVarianceThresholdWorkingMinutes);
        Assert.Null(options.HybridPrediction.AiTechnicalUpperBoundWorkingMinutes);
    }

    [Theory]
    [InlineData(1, null, true)]
    [InlineData(1, 1, true)]
    [InlineData(0, null, false)]
    [InlineData(-1, null, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -1, false)]
    public void OptionsValidation_EnforcesPositiveDurations(
        long procurementFallback,
        int? shippingFallback,
        bool isValid)
    {
        var values = ValidWorkingCalendarValues();
        values[$"{MvpAssumptionsOptions.SectionName}:procurement:fallbackDurationMinutes"] = procurementFallback.ToString();
        values[$"{MvpAssumptionsOptions.SectionName}:shipping:fallbackDurationMinutes"] = shippingFallback?.ToString();

        AssertValidity(values, isValid);
    }

    [Fact]
    public void OptionsValidation_AcceptsConsistentWorkingCalendar()
    {
        AssertValidity(ValidWorkingCalendarValues(), isValid: true);
    }

    [Fact]
    public void OptionsValidation_RejectsStartTimeNotBeforeEndTime()
    {
        var values = ValidWorkingCalendarValues();
        values[$"{MvpAssumptionsOptions.SectionName}:workingCalendar:startTime"] = "17:00";
        values[$"{MvpAssumptionsOptions.SectionName}:workingCalendar:endTime"] = "08:00";

        AssertValidity(values, isValid: false);
    }

    [Fact]
    public void OptionsValidation_RejectsBreakWindowOutsideShift()
    {
        var values = ValidWorkingCalendarValues();
        values[$"{MvpAssumptionsOptions.SectionName}:workingCalendar:breakStartTime"] = "07:00";

        AssertValidity(values, isValid: false);
    }

    [Fact]
    public void OptionsValidation_RejectsBreakMinutesMismatch()
    {
        var values = ValidWorkingCalendarValues();
        values[$"{MvpAssumptionsOptions.SectionName}:workingCalendar:breakMinutes"] = "30";

        AssertValidity(values, isValid: false);
    }

    [Fact]
    public void OptionsValidation_RejectsNetMinutesPerDayMismatch()
    {
        var values = ValidWorkingCalendarValues();
        values[$"{MvpAssumptionsOptions.SectionName}:workingCalendar:netMinutesPerDay"] = "400";

        AssertValidity(values, isValid: false);
    }

    [Fact]
    public void OptionsValidation_RejectsEmptyWorkingDays()
    {
        var values = ValidWorkingCalendarValues();
        values.Remove($"{MvpAssumptionsOptions.SectionName}:workingCalendar:workingDays:0");
        values.Remove($"{MvpAssumptionsOptions.SectionName}:workingCalendar:workingDays:1");
        values.Remove($"{MvpAssumptionsOptions.SectionName}:workingCalendar:workingDays:2");
        values.Remove($"{MvpAssumptionsOptions.SectionName}:workingCalendar:workingDays:3");
        values.Remove($"{MvpAssumptionsOptions.SectionName}:workingCalendar:workingDays:4");

        AssertValidity(values, isValid: false);
    }

    private static Dictionary<string, string?> ValidWorkingCalendarValues()
    {
        var prefix = $"{MvpAssumptionsOptions.SectionName}:workingCalendar";
        var workingDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        var values = new Dictionary<string, string?>
        {
            [$"{prefix}:startTime"] = "08:00",
            [$"{prefix}:endTime"] = "17:00",
            [$"{prefix}:breakStartTime"] = "12:00",
            [$"{prefix}:breakEndTime"] = "13:00",
            [$"{prefix}:breakMinutes"] = "60",
            [$"{prefix}:netMinutesPerDay"] = "480",
            [$"{MvpAssumptionsOptions.SectionName}:procurement:fallbackDurationMinutes"] = "960",
            [$"{MvpAssumptionsOptions.SectionName}:shipping:fallbackDurationMinutes"] = null
        };
        for (var i = 0; i < workingDays.Length; i++)
        {
            values[$"{prefix}:workingDays:{i}"] = workingDays[i];
        }

        return values;
    }

    private static void AssertValidity(Dictionary<string, string?> values, bool isValid)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddMvpAssumptionsOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var resolve = () => provider.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;

        if (isValid)
        {
            _ = resolve();
        }
        else
        {
            Assert.Throws<OptionsValidationException>(resolve);
        }
    }

    private static void AssertSingleProperty(JsonElement element, string name, JsonValueKind valueKind)
    {
        var properties = element.EnumerateObject().ToArray();
        Assert.Single(properties);
        Assert.Equal(name, properties[0].Name);
        Assert.Equal(valueKind, properties[0].Value.ValueKind);
    }
}
